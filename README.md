# Extract text from PDFs and generate embeddings

This sample shows how to extract plain text from each page of a set of PDF files in an Azure Blob Storage container, generate embeddings for each page using the [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/ai-services/openai-service) and push all the data to an Azure Storage Table.

## Usage

```bash
export OPENAI_ENDPOINT="https://[service-name].openai.azure.com/"
export OPENAI_KEY="..."
export BLOB_CONNECTION_STRING="..."
export BLOB_CONTAINER_NAME="blob-container-with-pdfs"
export OPENAI_DEPLOYMENT_NAME="openai-deployment-name"

dotnet run
```
