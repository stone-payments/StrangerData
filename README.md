# StrangerData - A .NET database populator for testing purposes

## Project Description ##
StrangerData is a tool designed to automatically fills your database with random data to make your unit/integration tests faster.  
The generator will auto maps all foreign keys and generates records to related tables.

## Getting Started ##
1. Install StrangerData with NuGet Package Manager:
> Install-Package StrangerData

2. Install your database dialect, example:
> Install-Package StrangerData.SqlServer

3. Configure the required connection strings.

To start generating your test data, create a new DataFactory object:
```csharp
using StrangerData;
using StrangerData.SqlServer;
...
var dataFactory = new DataFactory<SqlServerDialect>("MyConnectionString");
```

## Usage ##

Consider the example schema:

#### Person Table


| Column | Data Type | PK | FK |
| --- | --- | --- | --- |
| Id | INT | True | False |
| Name | VARCHAR(20) | False | False |
| Email | VARCHAR(50) | False | False |
| Age | INT | False | False |
| TeamId | INT | False | Team(Id) |


#### Team Table


| Column | Data Type | PK | FK |
| --- | --- | --- | --- |
| Id | INT | True | False |
| Name | VARCHAR(20) | False | False |


### 1. Creates a single record:
```csharp
...
IDicionary<string, object> record = dataFactory.CreateOne("dbo.Person");
```
The method will creates an record in the group table, and associates it to the created user. The dictionary will contains:
```json
{
  "Id": "Generated User's Id",
  "Name": "Random string",
  "Email": "Random string",
  "Age": "Random integer number",
  "TeamId": "Id from generated group record"
}
```
So you can specify your custom values. Do following:
```csharp
User user = dataFactory.CreateOne("dbo.Person", t => {
    t.WithValue("Name", "Will Byers");
});
```

The dictionary will contains:
```json
{
  "Id": "Generated User's Id",
  "Name": "Will Byers",
  "Email": "Random string",
  "Age": "Random integer number",
  "TeamId": "Id from generated group record"
}
```

### 2. Delete generated records:
To delete all generated records, just run:
```csharp
...
dataFactory.TearDown();
...
```

We suggest to use the TearDown() method inside yor Finally scope. This way it will run even if you code crashes on running, avoiding to have dirty data on your database.
