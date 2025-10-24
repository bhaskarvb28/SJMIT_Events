import json
import boto3
import uuid
from datetime import datetime
from boto3.dynamodb.conditions import Key, Attr

dynamodb = boto3.resource("dynamodb")
events_table = dynamodb.Table("Events")
semesters_table = dynamodb.Table("Semesters")  # To validate semester exists


def lambda_handler(event, context):
    # Enable CORS for all responses
    headers = {
        "Access-Control-Allow-Origin": "*",
        "Access-Control-Allow-Headers": "Content-Type",
        "Access-Control-Allow-Methods": "GET,POST,PUT,DELETE,OPTIONS"
    }

    try:
        http_method = event.get("httpMethod", "")

        if http_method == "OPTIONS":
            return {"statusCode": 200, "headers": headers, "body": json.dumps({"message": "CORS preflight"})}

        if http_method == "GET":
            return handle_get(event, headers)
        elif http_method == "POST":
            return handle_post(event, headers)
        elif http_method == "PUT":
            return handle_put(event, headers)
        elif http_method == "DELETE":
            return handle_delete(event, headers)
        else:
            return {"statusCode": 405, "headers": headers, "body": json.dumps({"error": "Method not allowed"})}

    except Exception as e:
        return {"statusCode": 500, "headers": headers, "body": json.dumps({"error": str(e)})}


def handle_get(event, headers):
    """Fetch events with flexible query parameters"""
    try:
        params = event.get("queryStringParameters") or {}

        # 1️⃣ Fetch a specific event by ID (date optional for validation)
        if "id" in params:
            resp = events_table.get_item(Key={"EventId": params["id"]})
            item = resp.get("Item")
            if not item:
                return {"statusCode": 404, "headers": headers, "body": json.dumps({"error": "Event not found"})}
            if "date" in params and item["Date"] != params["date"]:
                return {"statusCode": 404, "headers": headers, "body": json.dumps({"error": "Event not found"})}
            return {"statusCode": 200, "headers": headers, "body": json.dumps(item)}

        # 2️⃣ Fetch all events on a specific date
        elif "date" in params:
            resp = events_table.query(
                IndexName="DateIndex",
                KeyConditionExpression=Key("Date").eq(params["date"])
            )
            return {"statusCode": 200, "headers": headers, "body": json.dumps(resp["Items"])}

        # 3️⃣ Fetch events in a date range
        elif "startDate" in params and "endDate" in params:
            resp = events_table.scan(FilterExpression=Attr("Date").between(params["startDate"], params["endDate"]))
            return {"statusCode": 200, "headers": headers, "body": json.dumps(resp["Items"])}

        # 4️⃣ Fetch by semesterId
        elif "semesterId" in params:
            resp = events_table.query(
                IndexName="SemesterIndex",
                KeyConditionExpression=Key("semesterId").eq(params["semesterId"])
            )
            return {"statusCode": 200, "headers": headers, "body": json.dumps(resp["Items"])}

        # 5️⃣ Fetch by title
        elif "title" in params:
            resp = events_table.query(
                IndexName="TitleIndex",
                KeyConditionExpression=Key("Title").eq(params["title"])
            )
            items = resp.get("Items", [])
            if not items:  # fallback scan
                resp_scan = events_table.scan(FilterExpression=Attr("Title").eq(params["title"]))
                items = resp_scan.get("Items", [])
            return {"statusCode": 200, "headers": headers, "body": json.dumps(items)}

        else:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({
                    "error": "Please provide query parameters (date, startDate+endDate, id[+date], semesterId, or title)"
                })
            }

    except Exception as e:
        return {"statusCode": 500, "headers": headers, "body": json.dumps({"error": str(e)})}


def handle_post(event, headers):
    """Create a new event"""
    try:
        body = json.loads(event["body"])
        
        # Required fields
        date = body.get("date")
        title = body.get("title")
        semester_id = body.get("semesterId")
        
        # Optional fields
        description = body.get("description", "")
        event_type = body.get("type", "other")

        # Validate required fields
        if not date or not title or not semester_id:
            return {"statusCode": 400, "headers": headers,
                    "body": json.dumps({"error": "date, title, and semesterId are required"})}

        # Validate semester exists
        semester_response = semesters_table.get_item(Key={"semesterId": semester_id})
        if "Item" not in semester_response:
            return {"statusCode": 400, "headers": headers,
                    "body": json.dumps({"error": "Invalid semesterId - semester does not exist"})}

        # Generate unique ID and timestamp
        event_id = str(uuid.uuid4())
        created_at = datetime.utcnow().isoformat()

        # Create the event item with capitalized field names
        event_item = {
            "EventId": event_id,
            "Date": date,  # Use capitalized 'Date'
            "CreatedAt": created_at,
            "description": description,
            "semesterId": semester_id,
            "Title": title,  # Use capitalized 'Title'
            "type": event_type
        }

        events_table.put_item(Item=event_item)

        return {"statusCode": 201, "headers": headers,
                "body": json.dumps({"message": "Event created successfully", "eventId": event_id})}

    except Exception as e:
        return {"statusCode": 500, "headers": headers, "body": json.dumps({"error": str(e)})}

def handle_put(event, headers):
    """Update an event with partial updates, retaining existing values for unspecified fields"""
    try:
        body = json.loads(event.get("body", "{}"))
        event_id = body.get("eventId")

        if not event_id:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "eventId is required"})
            }

        # Check if the event exists
        existing_resp = events_table.get_item(Key={"EventId": event_id})
        if "Item" not in existing_resp:
            return {
                "statusCode": 404,
                "headers": headers,
                "body": json.dumps({"error": "Event not found"})
            }

        existing_item = existing_resp["Item"]

        # Map 'title' to 'Title' and 'date' to 'Date' for DynamoDB
        if "title" in body:
            body["Title"] = body.pop("title")
        if "date" in body:
            body["Date"] = body.pop("date")

        # Define allowed fields for update (use capitalized 'Date')
        allowed_fields = {"Date", "description", "semesterId", "Title", "type"}
        update_fields = {k: v for k, v in body.items() if k != "eventId" and k in allowed_fields}

        if not update_fields:
            return {
                "statusCode": 400,
                "headers": headers,
                "body": json.dumps({"error": "No valid fields to update"})
            }

        # Validate semesterId if provided
        if "semesterId" in update_fields:
            semester_response = semesters_table.get_item(Key={"semesterId": update_fields["semesterId"]})
            if "Item" not in semester_response:
                return {
                    "statusCode": 400,
                    "headers": headers,
                    "body": json.dumps({"error": "Invalid semesterId - semester does not exist"})
                }

        # Build update expression dynamically
        update_expression = "SET " + ", ".join(f"#{k} = :{k}" for k in update_fields)
        expression_attr_names = {f"#{k}": k for k in update_fields}
        expression_attr_values = {f":{k}": v for k, v in update_fields.items()}

        # Perform the update
        events_table.update_item(
            Key={"EventId": event_id},
            UpdateExpression=update_expression,
            ExpressionAttributeNames=expression_attr_names,
            ExpressionAttributeValues=expression_attr_values
        )

        return {
            "statusCode": 200,
            "headers": headers,
            "body": json.dumps({"message": "Event updated successfully"})
        }

    except Exception as e:
        return {
            "statusCode": 500,
            "headers": headers,
            "body": json.dumps({"error": str(e)})
        }
      

def handle_delete(event, headers):
    """Delete an event"""
    try:
        body = json.loads(event["body"])
        event_id = body["eventId"]
        date = body.get("date")

        existing = events_table.get_item(Key={"EventId": event_id})
        if "Item" not in existing:
            return {"statusCode": 404, "headers": headers, "body": json.dumps({"error": "Event not found"})}

        if date and existing["Item"]["Date"] != date:
            return {"statusCode": 404, "headers": headers, "body": json.dumps({"error": "Event not found"})}

        events_table.delete_item(Key={"EventId": event_id})
        return {"statusCode": 200, "headers": headers, "body": json.dumps({"message": "Event deleted successfully"})}

    except Exception as e:
        return {"statusCode": 500, "headers": headers, "body": json.dumps({"error": str(e)})}
