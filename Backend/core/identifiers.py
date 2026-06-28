import uuid


def new_user_uuid() -> str:
    return str(uuid.uuid4())


def new_transaction_id() -> str:
    return str(uuid.uuid4())
