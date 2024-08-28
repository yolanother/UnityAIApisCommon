# Unity AI Apis Common
Foundation library for a set of LLM based APIs. This is the common library that provides the base classes and shared functionality that any of the different LLM implementations may use. On its own this library doesn't do much. You will want to grab one of the LLM implmenentations.

## Implementations
[Open AI](https://github.com/yolanother/UnityOpenAIAPI) - Works for any API that is structured on the Open AI based rest APIs.
[Gemini](https://github.com/yolanother/UnityGeminiAPI) - Provides an implementation for Google's Gemini based API

## Installation
### Option 1: Package Manager
![image](https://github.com/user-attachments/assets/338cb582-3946-4d83-957f-78d3685ece75)
1. Add Unity AI APIs Common via package manager's git urls (https://github.com/yolanother/UnityAIApisCommon.git)
2. Import the implementation api you would like to use
   ex: Add Unity Open AI API in package manager (https://github.com/yolanother/UnityOpenAIAPI.git)

### Option 2: Clone to Packages
1. Open your Packages directory in your project
2. Clone the common lib: git clone https://github.com/yolanother/UnityAIApisCommon
3. Clone the implementation plugin you'd like to use
ex: Clone the Open AI api lib: git clone https://github.com/yolanother/UnityOpenAIAPI

