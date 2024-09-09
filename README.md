### Overview

This service acts as a foundational layer for all backend services in the overall microservices architecture . It includes advanced tools and structures designed to support the development of future services, with a focus on flexibility and configurability .

### Key Features

1. **Event and Message Handling** :
    - Provides advanced tools for working with events and messages across services .
    - Highly configurable to ensure seamless integration with messaging systems such as `RabbitMQ` and `Kafka` .

2. **Distributed Caching** :
    - Tools for better and more efficient use of distributed caches like `Redis` .

3. **Monitoring Tools** :
    - This project integrates several powerful tools for monitoring all errors, requests, and more. These tools are essential for maintaining visibility into the system's health and performance, ensuring that any issues are quickly identified and addressed
      In the following sections, we will provide a detailed explanation of how to use these monitoring tools and their functionality. You will learn how to effectively track errors, monitor incoming requests, and gain insights into the overall performance of your services .

4. **Infrastructure Tools** :
    - Includes tools for leveraging .NET infrastructure capabilities more effectively .
    - Supports patterns such as Middleware and Mediator with high configurability for professional use .

### Getting Started

To begin, we'll cover how to use the implemented tools within this codebase .

Each layer in this project, such as the **Domain Layer**, **UseCase Layer**, and others, has its own unique `PackageId` . These layers are packaged for use in public Microsoft NuGet repositories or private/company-specific NuGet servers . This means that each layer must be packaged and uploaded to the NuGet server, allowing other services to consume these packages accordingly within their own layers .

Below is a simple example illustrating how this works :

#### Example:

- **Domain Layer of TicketService** (usage) :
   - Depends on the **Domain Layer package** of `Domic-CoreService` from the NuGet server .

In this architecture, services utilize the packages of various layers (like Domain or UseCase layers) by referencing the respective NuGet packages . This modular approach allows for better reusability, maintainability, and separation of concerns within your microservices .

In the following sections, we will explain in detail how to package and publish each layer and how to integrate these packages into other services .

---

### Mediator ( for handle CQS | CQRS )

To begin and understand how to utilize the tools provided by this project (Domic), let's start with the **Mediator** tool .

To use the Mediator tool (which implements the Mediator pattern), you need to follow the steps shown in the images below, along with brief explanations for each part .

1 . As shown in the code below ( sample ), to define **Commands** within the project, you should follow these steps. First, create a class for your Command, and then inherit from the interface implemented in `Domic-CoreService` .

```
public class CreateUserCommand : ICommand<string> //any result type
{
    //some properties
}
```

2 . For the **CommandHandler** section, you should follow the steps shown in the image below . First, implement the corresponding Handler class and inherit from the `ICommandHandler` interface provided in `Domic-CoreService`. For implementing your core logic, you have two methods at your disposal : `Handle` and `HandleAsync`. Depending on your requirements, you can choose to use either of these methods .

```
public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    public CreateCommandHandler(){}

    public string Handle(CreateCommand command)
    {
        //logic
    }

    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
        //logic

        return Task.CompleteTask;
    }
}

```

3 . The same applies to the **Query** section . To implement your Query logic, you should use the `IQuery` and `IQueryHandler` interfaces provided in `Domic-CoreService`, just as you did for the Command section .

```
public class ReadAllUserQuery : IQuery<UsersDto> //any result type
{
}

public class ReadAllUserQueryHandler : IQueryHandler<ReadAllUserQuery, UsersDto>
{
    public ReadAllUserQueryHandler(){}

    public UsersDto Handle(ReadAllPaginatedQuery query)
    {
        //logic
    }

    public Task<UsersDto> HandleAsync(ReadAllPaginatedQuery query, CancellationToken cancellationToken)
    {
        //logic

        return Task.CompleteTask;
    }
}

```
