import json
import boto3
import uuid
from boto3.dynamodb.conditions import Attr

dynamodb = boto3.resource("dynamodb")
semesters_table = dynamodb.Table("Semesters")
events_table = dynamodb.Table("Events")

def lambda_handler(event, context):
    # Enable CORS for all responses
    headers = {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Headers": "Content-Type",
        "Access-Control-Allow-Methods": "GET,POST,PUT,DELETE,OPTIONS"
    }
    
    try:
        # Support both REST API (httpMethod) and HTTP API v2 (requestContext.http.method)
        http_method = event.get("httpMethod") or event.get("requestContext", {}).get("http", {}).get("method", "")
        
        # Normalize to uppercase
        http_method = http_method.upper() if http_method else ""

        # Handle OPTIONS request for CORS preflight
        if http_method == "OPTIONS":
            return {
                "statusCode": 200,
                "headers": headers,
                "body": json.dumps({"message": "CORS preflight"})
            }
        
        # Route based on HTTP method
        if http_method == "GET":
            return handle_get(event, headers)
        elif http_method == "POST":
            return handle_post(event, headers)
        elif http_method == "PUT":
            return handle_put(event, headers)
        elif http_method == "DELETE":
            return handle_delete(event, headers)
        else:
            return {
                "statusCode": 405,
                "headers": headers,
                "body": json.dumps({"error": f"Method not allowed: {http_method}"})
            }
            
    except Exception as e:
        return {
            "statusCode": 500,
            "headers": headers,
            "body": json.dumps({"error": str(e), "event": event})
        }

def handle_get(event, headers):
    """Get semesters with optional filtering via query parameters"""
    try:
        params = event.get("queryStringParameters") or {}

        # Filter by semesterId (exact match)
        if "semesterId" in params:
            resp = semesters_table.get_item(Key={"semesterId": params["semesterId"]})
            item = resp.get("Item")
            if not item:
                return {
                    "statusCode": 404,
                    "headers": headers,
                    "body": json.dumps({"error": "Semester not found"})
                }
            return {
                "statusCode": 200,
                "headers": headers,
                "body": json.dumps({
                    "semesters": [item],  # Wrap single item in array
                    "count": 1
                })
            }

        # Filter by isCurrent
        if "isCurrent" in params:
            is_current = str(params["isCurrent"]).lower() == "true"
            resp = semesters_table.scan(
                FilterExpression=Attr("isCurrent").eq(is_current)
            )
            semesters = resp.get("Items", [])
            semesters.sort(key=lambda x: x.get("startDate", ""), reverse=True)
            
            return {
                "statusCode": 200,
                "headers": headers,
                "body": json.dumps({
                    "semesters": semesters,  # Consistent structure
                    "count": len(semesters)
                })
            }

        # Default -> get all semesters
        response = semesters_table.scan()
        semesters = response.get("Items", [])
        semesters.sort(key=lambda x: x.get("startDate", ""), reverse=True)

        return {
            "statusCode": 200,
            "headers": headers,
            "body": json.dumps({
                "semesters": semesters,
                "count": len(semesters)
            })
        }

    except Exception as e:
        return {
            "statusCode": 500,
            "headers": headers,
            "body": json.dumps({"error": str(e)})
        }

def handle_post(event, headers):
    """Create a new semester"""
    try:
        body = json.loads(event.get("body", "{}"))
        name = body["name"]
        start_date = body["startDate"]
        end_date = body["endDate"]
        is_current = body.get("isCurrent", False)
        
        # Validate required fields
        if not name or not start_date or not end_date:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "Name, startDate, and endDate are required"})
            }
        
        # Validate date order
        if start_date >= end_date:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "Start date must be before end date"})
            }
        
        # If new semester is marked as current, reset any existing ones
        if is_current:
            reset_current_semesters()
        
        # Generate unique semester ID
        semester_id = str(uuid.uuid4())
        
        # Create the semester
        semesters_table.put_item(
            Item={
                "semesterId": semester_id,
                "name": name,
                "startDate": start_date,
                "endDate": end_date,
                "isCurrent": is_current
            }
        )
        
        return {
            "statusCode": 201,
            "headers": headers,
            "body": json.dumps({
                "message": "Semester created successfully",
                "semesterId": semester_id
            })
        }
        
    except json.JSONDecodeError:
        return {
            "statusCode": 400,
            "headers": headers,
            "body": json.dumps({"error": "Invalid JSON in request body"})
        }
    except KeyError as e:
        return {
            "statusCode": 400,
            "headers": headers,
            "body": json.dumps({"error": f"Missing required field: {str(e)}"})
        }
    except Exception as e:
        return {
            "statusCode": 500,
            "headers": headers,
            "body": json.dumps({"error": str(e)})
        }

def handle_put(event, headers):
    """Update an existing semester (partial update allowed)"""
    try:
        body = json.loads(event.get("body", "{}"))
        semester_id = body.get("semesterId")

        if not semester_id:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "semesterId is required"})
            }

        # Fetch existing record
        existing = semesters_table.get_item(Key={"semesterId": semester_id})
        if "Item" not in existing:
            return {
                "statusCode": 404,
                "headers": headers,
                "body": json.dumps({"error": "Semester not found"})
            }
        
        current_item = existing["Item"]

        # Merge fields (keep old if not provided)
        name = body.get("name", current_item["name"])
        start_date = body.get("startDate", current_item["startDate"])
        end_date = body.get("endDate", current_item["endDate"])
        is_current = body.get("isCurrent", current_item.get("isCurrent", False))

        # Validate dates (only if both are provided)
        if start_date and end_date and start_date >= end_date:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "Start date must be before end date"})
            }

        # If semester is being marked as current, reset others
        if is_current:
            reset_current_semesters()

        # Update the semester
        semesters_table.update_item(
            Key={"semesterId": semester_id},
            UpdateExpression="SET #name = :name, startDate = :start_date, endDate = :end_date, isCurrent = :is_current",
            ExpressionAttributeNames={"#name": "name"},
            ExpressionAttributeValues={
                ":name": name,
                ":start_date": start_date,
                ":end_date": end_date,
                ":is_current": is_current
            }
        )

        return {
            "statusCode": 200,
            "headers": headers,
            "body": json.dumps({
                "message": "Semester updated successfully",
                "semesterId": semester_id
            })
        }

    except json.JSONDecodeError:
        return {
            "statusCode": 400,
            "headers": headers,
            "body": json.dumps({"error": "Invalid JSON in request body"})
        }
    except Exception as e:
        return {
            "statusCode": 500,
            "headers": headers,
            "body": json.dumps({"error": str(e)})
        }

def handle_delete(event, headers):
    """Delete a semester and all associated events (only if not active/current)"""
    try:
        body = json.loads(event.get("body", "{}"))
        semester_id = body["semesterId"]
        
        if not semester_id:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "semesterId is required"})
            }
        
        # Check if semester exists before deleting
        existing = semesters_table.get_item(Key={"semesterId": semester_id})
        if "Item" not in existing:
            return {
                "statusCode": 404,
                "headers": headers,
                "body": json.dumps({"error": "Semester not found"})
            }

        semester = existing["Item"]

        # Prevent deleting active semester
        if semester.get("isCurrent", False):
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "Cannot delete the active/current semester"})
            }
        
        # Delete all events associated with this semester
        deleted_event_count = 0
        try:
            # Scan for events with matching semesterId
            response = events_table.scan(
                FilterExpression=Attr("semesterId").eq(semester_id)
            )
            events = response.get("Items", [])
            
            # Delete each event
            for event in events:
                if "EventId" not in event:
                    print(f"Skipping event with missing EventId: {event}")
                    continue
                try:
                    events_table.delete_item(
                        Key={"EventId": event["EventId"]}  # Updated to use EventId
                    )
                    deleted_event_count += 1
                except Exception as e:
                    print(f"Failed to delete event {event.get('EventId')}: {str(e)}")
                    continue  # Continue deleting other events even if one fails
            
            # Handle paginated results (in case there are more events)
            while "LastEvaluatedKey" in response:
                response = events_table.scan(
                    FilterExpression=Attr("semesterId").eq(semester_id),
                    ExclusiveStartKey=response["LastEvaluatedKey"]
                )
                events = response.get("Items", [])
                for event in events:
                    if "EventId" not in event:
                        print(f"Skipping event with missing EventId: {event}")
                        continue
                    try:
                        events_table.delete_item(
                            Key={"EventId": event["EventId"]}  # Updated to use EventId
                        )
                        deleted_event_count += 1
                    except Exception as e:
                        print(f"Failed to delete event {event.get('EventId')}: {str(e)}")
                        continue
        
        except Exception as e:
            return {
                "statusCode": 500,
                "headers": headers,
                "body": json.dumps({"error": f"Failed to scan or delete events: {str(e)}"})
            }
        
        # Delete the semester
        semesters_table.delete_item(Key={"semesterId": semester_id})
        
        return {
            "statusCode": 200,
            "headers": headers,
            "body": json.dumps({
                "message": "Semester and associated events deleted successfully",
                "semesterId": semester_id,
                "deletedEventCount": deleted_event_count
            })
        }
        
    except json.JSONDecodeError:
        return {
            "statusCode": 400,
            "headers": headers,
            "body": json.dumps({"error": "Invalid JSON in request body"})
        }
    except KeyError as e:
        return {
            "statusCode": 400,
            "headers": headers,
            "body": json.dumps({"error": f"Missing required field: {str(e)}"})
        }
    except Exception as e:
        return {
            "statusCode": 500,
            "headers": headers,
            "body": json.dumps({"error": str(e)})
        }

def reset_current_semesters():
    """Helper function to reset all semesters' isCurrent flag to False"""
    try:
        scan_response = semesters_table.scan(FilterExpression=Attr("isCurrent").eq(True))
        for item in scan_response.get("Items", []):
            semesters_table.update_item(
                Key={"semesterId": item["semesterId"]},
                UpdateExpression="SET isCurrent = :val",
                ExpressionAttributeValues={":val": False}
            )
    except Exception as e:
        print(f"Error resetting current semesters: {str(e)}")
