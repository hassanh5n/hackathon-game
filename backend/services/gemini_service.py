import google.generativeai as genai
import asyncio
from backend.config import GEMINI_API_KEY

genai.configure(api_key=GEMINI_API_KEY)

async def generate(prompt: str) -> str:
    """
    Calls the Gemini API with the prompt.
    Returns the text response.
    Has try/except — on failure returns empty string and prints error.
    Has 10 second timeout.
    """
    try:
        model = genai.GenerativeModel("gemini-2.0-flash")
        
        # Using a wrapper to handle timeout since the library doesn't have a direct async timeout param easily accessible in this way
        # But we can use asyncio.wait_for
        response = await asyncio.wait_for(
            asyncio.to_thread(model.generate_content, prompt),
            timeout=10.0
        )
        
        if response and response.text:
            return response.text
        return ""
    except asyncio.TimeoutError:
        print("Gemini API call timed out.")
        return ""
    except Exception as e:
        print(f"Error calling Gemini API: {e}")
        return ""
