import firebase_admin
from firebase_admin import credentials, firestore
from models.nemesis_state import NemesisState
import os

db = None

def initialize_firebase():
    global db
    if not firebase_admin._apps:
        try:
            # First try standard initialization (which uses GOOGLE_APPLICATION_CREDENTIALS)
            firebase_admin.initialize_app()
        except Exception as e:
            print(f"Failed to initialize Firebase: {e}")
                 
    if db is None:
        try:
            db = firestore.client()
        except Exception as e:
            print(f"Failed to initialize Firestore client: {e}")

def save_nemesis_state(device_id: str, state: NemesisState) -> bool:
    if not db:
        return False
    try:
        doc_ref = db.collection('nemesis_states').document(device_id)
        # Convert state model to dict, exclude any non-serializable fields if needed
        state_dict = state.model_dump()
        doc_ref.set(state_dict)
        return True
    except Exception as e:
        print(f"Error saving to Firestore: {e}")
        return False

def get_nemesis_state(device_id: str) -> NemesisState:
    if not db:
        return None
    try:
        doc_ref = db.collection('nemesis_states').document(device_id)
        doc = doc_ref.get()
        if doc.exists:
            data = doc.to_dict()
            return NemesisState(**data)
        return None
    except Exception as e:
        print(f"Error getting from Firestore: {e}")
        return None

def delete_nemesis_state(device_id: str) -> bool:
    if not db:
        return False
    try:
        db.collection('nemesis_states').document(device_id).delete()
        return True
    except Exception as e:
        print(f"Error deleting from Firestore: {e}")
        return False
