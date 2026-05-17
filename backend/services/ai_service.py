import httpx
import asyncio
from config import GROQ_API_KEY

async def generate(prompt: str, system: str = "") -> str:
    """
    Calls the Groq API with the prompt.
    Returns the text response.
    Has try/except — on failure returns empty string and prints error.
    Has 10 second timeout.
    """
    if not GROQ_API_KEY or GROQ_API_KEY == "your_groq_key_here":
        print("Error: GROQ_API_KEY is not set in .env")
        return ""
        
    url = "https://api.groq.com/openai/v1/chat/completions"
    headers = {
        "Authorization": f"Bearer {GROQ_API_KEY}",
        "Content-Type": "application/json"
    }
    messages = []
    if system:
        messages.append({"role": "system", "content": system})
    messages.append({"role": "user", "content": prompt})

    data = {
        "model": "llama-3.3-70b-versatile",
        "messages": messages,
        "temperature": 0.75
    }
    
    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(url, headers=headers, json=data, timeout=10.0)
            if response.status_code != 200:
                print(f"Groq API Error {response.status_code}: {response.text}")
            response.raise_for_status()
            result = response.json()
            if "choices" in result and len(result["choices"]) > 0:
                return result["choices"][0]["message"]["content"]
            return ""
    except asyncio.TimeoutError:
        print("Groq API call timed out.")
        return ""
    except Exception as e:
        print(f"Error calling Groq API: {e}")
        return ""
