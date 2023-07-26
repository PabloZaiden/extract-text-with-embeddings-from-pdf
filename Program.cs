using Azure;
using Azure.AI.OpenAI;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Azure.Storage.Blobs;

var oaiEndpoint = GetEnv("OPENAI_ENDPOINT");
var oaiKey = GetEnv("OPENAI_KEY");
var blobConnectionString = GetEnv("BLOB_CONNECTION_STRING");
var containerName = GetEnv("BLOB_CONTAINER_NAME");
var deploymentName = GetEnv("OPENAI_DEPLOYMENT_NAME");

var credentials = new AzureKeyCredential(oaiKey);

var openAIClient = new OpenAIClient(new Uri(oaiEndpoint), credentials);

BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);
var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

var blobs = containerClient.GetBlobs();

List<Data> allTheData = new List<Data>();

foreach (var blob in blobs)
{
    System.Console.WriteLine("Blob name: " + blob.Name);
    if (!blob.Name.EndsWith(".pdf"))
    {
        System.Console.WriteLine("Not a PDF. Skipping...");
        continue;
    }

    var blobClient = new BlobClient(blobConnectionString, containerName, blob.Name);
    var response = blobClient.DownloadContent();
    var bytes = response.Value.Content.ToArray();

    using (var document = UglyToad.PdfPig.PdfDocument.Open(bytes))
    {
        foreach (var page in document.GetPages())
        {
            var text = String.Join(" ", page.GetWords());
            System.Console.WriteLine($"Page {page.Number}");

            if (String.IsNullOrWhiteSpace(text)) continue;
            var embeddingOptions = new EmbeddingsOptions(text);
            var returnValue = openAIClient.GetEmbeddings(deploymentName, embeddingOptions);

            List<float> embeddings = new List<float>();
            foreach (float item in returnValue.Value.Data[0].Embedding)
            {
                embeddings.Add(item);
            }

            var dataItem = new Data(
                blob.Name,
                page.Number,
                text,
                embeddings.ToArray()
            );

            allTheData.Add(dataItem);
        }
    }
}

var serviceClient = new TableServiceClient(blobConnectionString);
var tableName = "t" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
System.Console.WriteLine($"Creating table {tableName}...");
var tableClient = new TableClient(blobConnectionString, tableName);
TableItem table = serviceClient.CreateTableIfNotExists(tableName);
Console.WriteLine($"The created table's name is {table.Name}.");

foreach (var item in allTheData)
{
    var tableEntity = new TableEntity(item.FileName, item.FileName + item.PageNumber.ToString())
    {
        { nameof(Data.FileName), item.FileName },
        { nameof(Data.PageNumber), item.PageNumber },
        { nameof(Data.Content), item.Content },
        { nameof(Data.Embeddings), "[" + String.Join(",", item.Embeddings) + "]" }
    };

    System.Console.WriteLine($"Adding {item.FileName} page {item.PageNumber} to table {tableName}...");
    tableClient.AddEntity(tableEntity);
}

string GetEnv(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (String.IsNullOrWhiteSpace(value))
    {
        Environment.FailFast($"Environment variable {name} is not set.");
    }

    return value;
}

record Data(String FileName, int PageNumber, string Content, float[] Embeddings);