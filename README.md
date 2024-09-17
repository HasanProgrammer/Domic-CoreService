<div dir="rtl" style="font-family: IRANSans">

### 🏆 مقدمه

این پروژه، یک پروژه زیر ساختی برای تمام سرویس های توسعه داده شده بر اساس پروژه `Template` است و شامل ابزارهای کاربردی برای توسعه بهتر و پیشرفته تر سرویس های موجود در معماری میکروسرویس می باشد .

🔥 **توجه** : **تمامی این ابزارها به طور اختصاصی برای این پروژه طراحی و توسعه داده شده اند و دارای نمونه پیاده سازی مشابه نمی باشند .**

| پروژه                   | لینک                                                     |
|-------------------------|----------------------------------------------------------|
| `Domic-TemplateService` | https://github.com/HasanProgrammer/Domic-TemplateService |

برخی از قابلیت ها و امکانات پیاده سازی شده مطابق با استانداردهای روز مهندسی نرم افزار که در این سورس موجود است به شرح زیر می باشد :

| قابلیت               | توضیحات                                                                                                                                                                                                                                         |
|----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **CQRS**             | برخورداری از ابزار مناسب برای مدیریت اصل `CQS` و به طور دقیق تر الگوی `CQRS` با استفاده از ابزار `Mediator` که در اصل این ابزار پیاده کننده الگوی `Mediator` می باشد                                                                            |
| **AsyncCommand**     | برخورداری از قابلیت `Command` های `Async` مبتنی بر زیرساخت `Broker` و با استفاده از ابزار `RabbitMQ` برای مدیریت درخواست های `Fire&Forget`                                                                                                      |
| **DistributedCache** | برخورداری از ابزار مناسب برای مدیریت `Distributed Cache` مبتنی بر ابزار زیرساختی `Redis` برای اضافه کردن لایه `Cache` در پروژه                                                                                                                  |
| **EDA**              | برخورداری از ابزار مناسب برای مدیریت بهتر و بهینه تر `Event` و یا `Message` در ساختار پروژه ها مبتنی بر ابزارهای `RabbitMQ` و `Apache Kafka`                                                                                                    |
| **Logger**           | برخورداری از ابزار لاگ مرکزی یا همان سرویس ( `StateTracker` ) برای مدیریت لاگ خطاهای ایجاد شده در سرویس ها و لاگ رخدادها ( `Event Snapshot` ) و یا لاگ های ایجاد شده در سطح کدهای نوشته شده در سرویس ها با ابزار مربوطه ( `Logger` و `StreamLogger` ) |
| **RPC**              | برخورداری از زیرساخت مناسب برای مدیریت درخواست های مبتنی بر پروتکل `RPC` و براساس ابزار `gRPC`                                                                                                                                                  |
| **ServiceDiscovery** | برخورداری از ابزارهای مناسب برای دریافت و واکشی اطلاعات سرویس ها مبتنی بر سرویس `DiscoveryService` و بررسی سلامت سرویس ها ( `HealthCheck` ) براساس چرخه زمان بندی مشخص                                                                          |
| **AuthFilters**      | برخورداری از فیلتر های شخصی سازی شده برای مدیریت سطوح دسترسی ( `Permission` ) و مدیریت توکن های `Revoke` شده در ساختار `JWT`                                                                                                                    |
| **ExceptionHandler** | برخورداری از لایه مدیریت کننده خطاهای مدیریت شده و مدیریت نشده در لایه های مختلف پروژه و بازگشت پاسخ مناسب به `Client`                                                                                                                          |

---

### 🏆 ابزار `Mediator` برای مدیریت الگوی `CQRS`

در این پروژه برای آنکه بتوانید منطق های بخش `Command` و بخش `Query` خود را مجزا کنید می توانید این ابزار که با نام `Mediator` در این پروژه پیاده سازی شده است را مورد استفاده قرار دهید .
در ابتدا اجازه دهید نحوه پیاده سازی کلاس های مربوط به بخش `Command` و بخش `Query` را مورد ارزیابی قرار دهیم و در انتها به نحوه دسترسی به این منطق ها با استفاده از واسط `Mediator` می پردازیم .

1 . نحوه تعریف کلاس های مربوط به بخش `Command` مطابق زیر می باشد

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    public CreateCommandHandler(){}
    
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }

    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>
 
2 . برای تعریف کلاس های مربوط به لاجیک بخش `Query` هم می توانید مطابق دستورات زیر عمل نمایید

<div dir="ltr">

```csharp
public class ReadAllQuery : IQuery<Dto> //any result type
{
}

public class ReadAllQueryHandler : IQueryHandler<ReadAllQuery, Dto>
{
    public ReadAllQueryHandler(){}

    public Dto Handle(ReadAllQuery query)
    {
        //query
        
        return default;
    }
    
    public Task<Dto> HandleAsync(ReadAllQuery query, CancellationToken cancellationToken)
    {
        //query
        
        return Task.FromResult<Dto>(default);
    }
}
```

</div>

3 . فعال سازی ابزار `Mediator` در سرویس مربوطه

در نهایت برای فعال سازی این ابزار در سرویس خود ، می بایست در لایه `Presentation` و در فایل `Program.cs` مطابق دستورات زیر عمل نمایید .

<div dir="ltr">

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.RegisterCommandQueryUseCases();
```

</div>

🔥 **توجه** : **دقت داشته باشید که `Command` و `Query` های مربوطه می بایست در لایه `UseCase` سرویس های مربوطه تعریف شده و مورد استفاده قرار بگیرند**

---
 
### 🏆 قابلیت های پیشرفته ابزار `Mediator`

1 . استفاده از `WithTransactionAttribute`

از این `Attribute` برای مواقعی که نیاز دارید تا عملیات `Command` خود را در داخل یک `Transaction` مدیریت کنید، استفاده می شود که دارای یک `Property` تحت عنوان `IsolationLevel` می باشد که سطح قفل گزاری منطق شما را در داخل دیتابیس مدیریت می کند ( `Pessimistic Lock` ) .

در ابتدا برای استفاده از این ابزار می بایست در سطح لایه `Domain` سرویس مربوطه ، یک واسط پیاده سازی کرده که از واسط `ICoreCommandUnitOfWork` ارث بری کرده است، مطابق کد زیر :

<div dir="ltr">

```csharp
public interface ICommandUnitOfWork : ICoreCommandUnitOfWork;
```

</div>

سپس باید در لایه `Infrastructure` سرویس مربوطه این واسط پیاده سازی شود ، مطابق کد زیر :

<div dir="ltr">

```csharp
public class CommandUnitOfWork : ICommandUnitOfWork
{
    private readonly SQLContext   _context;
    private IDbContextTransaction _transaction;

    public CommandUnitOfWork(SQLContext context) => _context = context; //Resource

    public void Transaction(IsolationLevel isolationLevel) 
        => _transaction = _context.Database.BeginTransaction(isolationLevel); //Resource

    public async Task TransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = new CancellationToken())
    {
        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken); //Resource
    }

    public void Commit()
    {
        _context.SaveChanges();
        _transaction.Commit();
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
    }

    public void Rollback() => _transaction?.Rollback();

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
            return _transaction.RollbackAsync(cancellationToken);

        return Task.CompletedTask;
    }

    public void Dispose() => _transaction?.Dispose();

    public ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            return _transaction.DisposeAsync();

        return ValueTask.CompletedTask;
    }
}
```

</div>

🔥 **توجه** : **در نظر داشته باشید که این موارد به طور پیشفرض در سرویس `Template` پیاده سازی شده اند**

در ادامه برای استفاده از `Attribute` مربوطه می توانید مطابق کد زیر عمل نمایید .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    public CreateCommandHandler(){}

    [WithTransaction]
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }

    [WithTransaction]
    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>

🔥 **توجه** : **در صورتی که مقداری برای ویژگی `IsolationLevel` در این `Attribute` در نظر گرفته نشود، مقدار پیشفرض `ReadCommitted` لحاظ می گردد**

در ادامه برای استفاده از `Attribute` مربوطه با مقدار `IsolationLevel` مشخص می توانید مطابق کد زیر عمل نمایید .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    public CreateCommandHandler(){}

    [WithTransaction(IsolationLevel = IsolationLevel.RepeatableRead)]
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }

    [WithTransaction(IsolationLevel = IsolationLevel.RepeatableRead)]
    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>

🔥 **توجه** : **برای کارکرد صحیح `WithTransaction` در سرویس خود ، می بایست این `Attribute` را فعال سازی نمایید**

برای فعال سازی `WithTransaction` در سرویس خود می بایست در لایه `Presentation` و در فایل `Program.cs` دستورات زیر را اعمال نمایید .

<div dir="ltr">

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.RegisterCommandRepositories();
```

</div>

2 . استفاده از `WithValidationAttribute`

از این `Attribute` برای مواقعی که نیاز به اعتبارسنجی `Command` یا `Query` خود دارید استفاده می شود . برای شروع می بایست کلاس مربوط به `Validator` را ایجاد نمایید و سپس اقدام به گذاشتن `WithValidation` نمایید .

<div dir="ltr">

```csharp
public class CreateCommandValidator : IValidator<CreateCommand>
{
    public CreateCommandValidator(){}
    
    public object Validate(CreateCommand input)
    {
        //validations
        
        return default;
    }

    public Task<object> ValidateAsync(CreateCommand input, CancellationToken cancellationToken)
    {
        //validations
        
        return Task.FromResult(default(object));
    }
}
```

</div>

کدهای بالا برای موارد سمت `Query` نیز صدق می کند و برای این بخش هم می توان از دستورات بالا استفاده کرد ، حال می توان از این `Attribute` استفاده کرد .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    public CreateCommandHandler(){}

    [WithValidation]
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }

    [WithValidation]
    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>

🔥 **توجه** : **در کد بالا و در بخش مربوط به کلاس `Validator` مربوطه ، شما می توانید نتیجه متد `Validate` و یا `ValidateAsync` را که یک `object` می باشد در داخل `CommandHandler` مربوطه مورد استفاده قرار دهید**

برای این مهم ، کافی است که در قسمت `CommandHandler` خود یک متغیر از نوع `object` و با نام `validationResult_` و به شکل `readonly` ایجاد نمایید .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    private readonly object _validationResult;
    
    public CreateCommandHandler(){}

    [WithValidation]
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }

    [WithValidation]
    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>

3 . استفاده از `WithCleanCacheAttribute`

در قسمت `Command` در مواقعی نیاز دارید که پس از اجرا شدن لاجیک بخش مربوطه ، `Cache` مربوط به موجودیت مورد نظر را حذف نمایید تا مجدد در درخواست دیگری که برای قسمت `Query` مربوطه ارسال می شود ، `Cache` مربوطه ایجاد شود .
برای سهولت در این کار می توانید از این `Attribute` مطابق کد های زیر استفاده نمایید .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    public CreateCommandHandler(){}

    [WithCleanCache(Keies = "Key1|Key2|...")]
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }

    [WithCleanCache(Keies = "Key1|Key2|...")]
    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>

4 . استفاده از `WithPessimisticConcurrencyAttribute`

در مواقعی که نیاز دارید تا منطق بخش مربوط به `Command` خود را که یک `Critical Section` می باشد در داخل بلوک `lock` قرار دهید که تنها یک یا تعداد مشخصی `Thread` بتوانند به آن بخش `Critical` دسترسی داشته باشند ، می توانید از این `Attribute` استفاده نمایید .

🔥 **توجه** : **برای متد `Handle` باید در داخل `CommandHandler` خود یک متغیر از نوع `object` ایجاد نمایید و برای `HandleAsync` باید یک متغیر از نوع `SemaphoreSlim` ایجاد نمایید**

برای استفاده از این `Attribute` و در کنار متد `Handle` بخش مربوط به `Command` باید مطابق دستورات زیر عمل نمایید .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    private static object _lock = new();
    
    public CreateCommandHandler(){}

    [WithPessimisticConcurrency]
    public string Handle(CreateCommand command)
    {
       //logic
        
        return default;
    }
}
```

</div>

🔥 **توجه** : **در کد بالا ، حتما می بایست نام متغیر مربوط به کلید قفل گذاری ، `lock_` باشد**

برای استفاده از این `Attribute` و در کنار متد `HandleAsync` بخش مربوط به `Command` باید مطابق دستورات زیر عمل نمایید .

<div dir="ltr">

```csharp
public class CreateCommand : ICommand<string> //any result type
{
    //some properties
}

public class CreateCommandHandler : ICommandHandler<CreateCommand, string>
{
    private static SemaphoreSlim _asyncLock = new(1, 1); //custom count of thread
    
    public CreateCommandHandler(){}

    [WithPessimisticConcurrency]
    public Task<string> HandleAsync(CreateCommand command, CancellationToken cancellationToken)
    {
       //logic
        
       return Task.FromResult<string>(default);
    }
}
```

</div>

🔥 **توجه** : **در کد بالا ، حتما می بایست نام متغیر مربوط به کلید قفل گذاری ، `asyncLock_` باشد**

---

### 🏆 ابزار `ExternalDistributedCache` و `InternalDistributedCache` برای مدیریت `Cache`

برای مدیریت پیشرفته تر و خوانا تر `Cache` های نوشته شده در سطح پروژه که بر اساس دیتابیس `Redis` پیاده سازی شده است می توانید ، مطابق دستور العمل های زیر اقدام نمایید .

🔥 **توجه** : **از واسط `IInternalDistributedCache` برای `Redis` متعلق به سرویس جاری استفاده می شود و سرویس های دیگر به این `Cache` دسترسی ندارند**

🔥 **توجه** : **از واسط `IExternalDistributedCache` برای `Redis` متعلق به همه سرویس ها استفاده می شود ، درواقع پیاده کننده این واسط از `Redis` مشترک برای همه سرویس ها استفاده می کند**

1 . تعریف کلاس مربوط به منطق دیتای مورد نیاز برای `Cache`

در ابتدا ، شما می بایست کلاس مربوط به منطق `Cache` خود را مطابق دستورات زیر ایجاد نمایید .

<div dir="ltr">

```csharp
//for current service distributed cahce
public class MemoryCache : IInternalDistributedCacheHandler<List<Dto>>
{
    public MemoryCache(){}

    [Config(Key = 'Key', Ttl = 60 /*time to live based on minute*/)]
    public List<Dto> Set()
    {
        //query
        
        return new();
    }
    
    [Config(Key = 'Key', Ttl = 60 /*time to live based on minute*/)]
    public Task<List<Dto>> SetAsync(CancellationToken cancellationToken)
    {
        //query
        
        return Task.FromResult(new());
    }
}

//for all services distributed cahce ( global | share cahce )
public class MemoryCache : IExternalDistributedCacheHandler<List<Dto>>
{
    public MemoryCache(){}

    [Config(Key = 'Key', Ttl = 60 /*time to live based on minute*/)]
    public List<Dto> Set()
    {
        //query
        
        return new();
    }
    
    [Config(Key = 'Key', Ttl = 60 /*time to live based on minute*/)]
    public Task<List<Dto>> SetAsync(CancellationToken cancellationToken)
    {
        //query
        
        return Task.FromResult(new());
    }
}
```

</div>

🔥 **توجه** : **اگر در `ConfigAttribute` کدهای فوق ، مقداری برای `Ttl` تنظیم نکنید و یا این `Property` را 0 مقداردهی نمایید ، `Cache` مربوطه به شکل دائمی و بدون انقضا در `Redis` باقی خواهد ماند**

2 . فراخوانی `Cache` مربوطه در قسمت مورد نیاز

حال برای استفاده از مقدار `Cache` شده ( مطابق دستورات فوق ) می بایست ، از واسط متناسب با `InternalCache` و یا `ExternalCache` استفاده نمود . برای این مهم دو واسط `IInternalDistributedCacheMediator` و `IExternalDistributedCacheMediator` پیاده سازی شده اند که می توان از آنها مطابق دستورات زیر استفاده کرد .

<div dir="ltr">

```csharp
public class Query : IQuery<List<Dto>>
{
}

public class QueryHandler : IQueryHandler<Query, List<Dto>>
{
    private readonly IInternalDistributedCacheMediator _cacheMediator;

    public QueryHandler(IInternalDistributedCacheMediator cacheMediator) => _cacheMediator = cacheMediator;

    public List<Dto> Handle(Query query)
    {
        var result = _cacheMediator.Get<List<Dto>>(cancellationToken);

        return result;
    }
    
    public async Task<List<Dto>> HandleAsync(Query query, CancellationToken cancellationToken)
    {
        var result = await _cacheMediator.GetAsync<List<Dto>>(cancellationToken);

        return result;
    }
}
```

</div>

🔥 **توجه** : **برای فراخوانی `Cache` مورد نیاز ، همانطور که در کدهای فوق مشخص می باشد ، نیاز به ارسال کلید مربوطه به متد `<>Get` و `<>GetAsync` نمی باشد ، بلکه این متد از نوع ارسالی در قسمت `Generic` و تطابق آن با نوع در نظر گرفته شده در قسمت `Setter` ، داده ها را واکشی می کند**

3 . فعال سازی ابزار `DistributedCache` در سرویس مربوطه

در نهایت برای فعال سازی این قابلیت در سرویس خود ، می بایست در لایه `Presentation` و در فایل `Program.cs` از دستور زیر استفاده نمایید .

<div dir="ltr">

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.RegisterDistributedCaching();
```

</div>

برای تنظیمات مربوط به رشته اتصال و اطلاعات مربوط به `Cache` می بایست در سرویس مربوطه و در بخش `Properties` و در فایل مربوط به `launchSettings.json` و در قسمت `environmentVariables` کلید های زیر را اضافه نمایید .

<div dir="ltr">

```json
{
  "environmentVariables": {
    "E-RedisConnectionString": "", //external connection
    "I-RedisConnectionString": ""  //internal connection
  }
}
```

</div>

🔥 **توجه** : **دقت داشته باشید که منطق `Cache` های نوشته شده می بایست در لایه `UseCase` سرویس های مربوطه تعریف شده و مورد استفاده قرار بگیرند**

---

### 🏆 ابزار `MessageBroker` برای مدیریت الگوی معماری `EDA`

در ابتدا ، می بایست از ابزار پر کاربرد `MessageBroker` شروع نماییم که استفاده زیادی در سطح پروژه های مبتنی بر معماری میکروسرویس دارد .

برای استفاده از این ابزار و نیز برای استفاده از زیرساخت های پیاده سازی شده ( سرویس های تکمیلی آماده ) ، می بایست ابتدا به سرویس `Domic-TriggerService` مراجعه کرده و سپس به پروژه `Domic.Init.MessageBroker` رفته و این پروژه را اجرا گرفته تا تمام ساختارهای `Queue` و `Exchange` مربوط به سرویس های زیرساختی پروژه `Domic` ایجاد شوند .

حال بیایید به بررسی دقیق ابزار `MessageBroker` در پروژه `Domic` بیندازیم .

1 . نحوه ایجاد `Event` در سطح سرویس ها و مدیریت آنها برای ارسال به `Broker`

برای این مهم ابتدا باید به این نکته اشاره کرد که تمامی `Event` ها در لایه `Domain` سرویس ها ایجاد می شوند و از بیرون از این لایه تنها به استفاده و مدیریت این `Event` های ایجاد شده پرداخته می شود .

<div dir="ltr">

```csharp
//ExchangeType : Exchange.FanOut | Exchange.Direct | Exchange.Headers | Exchange.Topic

//FanOut-Exchange

//create event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange")]
public class Created : CreateDomainEvent<string> //any type of identity key
{
    //payload
}

//update event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange")]
public class Updated : UpdateDomainEvent<string> //any type of identity key
{
    //payload
}

//delete event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange")]
public class Deleted : DeleteDomainEvent<string> //any type of identity key
{
    //payload
}

```
</div>

<div dir="ltr">

```csharp
//ExchangeType : Exchange.FanOut | Exchange.Direct | Exchange.Headers | Exchange.Topic

//Direct-Exchange

//create event
[MessageBroker(ExchangeType = Exchange.Direct, Exchange = "exchange", Route = "route")]
public class Created : CreateDomainEvent<string> //any type of identity key
{
    //payload
}

//update event
[MessageBroker(ExchangeType = Exchange.Direct, Exchange = "exchange", Route = "route")]
public class Updated : UpdateDomainEvent<string> //any type of identity key
{
    //payload
}

//delete event
[MessageBroker(ExchangeType = Exchange.Direct, Exchange = "exchange", Route = "route")]
public class Deleted : DeleteDomainEvent<string> //any type of identity key
{
    //payload
}

```
</div>

🔥 **توجه** : **اگر چنانچه در کدهای بالا، سرویس مذکور علاوه بر تولید این رخداد ها ( `Producer` ) ، مصرف کننده این رخداد نیز باشد ( `Consumer` ) می بایست مطابق دستورات زیر عمل کرد**

<div dir="ltr">

```csharp
//FanOut-Exchange

//create event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange", Queue = "queue")]
public class Created : CreateDomainEvent<string> //any type of identity key
{
    //payload
}

//update event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange", Queue = "queue")]
public class Updated : UpdateDomainEvent<string> //any type of identity key
{
    //payload
}

//delete event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange", Queue = "queue")]
public class Deleted : DeleteDomainEvent<string> //any type of identity key
{
    //payload
}
```

</div>

<div dir="ltr">

```csharp
//Direct-Exchange

//create event
[MessageBroker(ExchangeType = Exchange.Direct, Exchange = "exchange", Route = "route", Queue = "queue")]
public class Created : CreateDomainEvent<string> //any type of identity key
{
    //payload
}

//update event
[MessageBroker(ExchangeType = Exchange.Direct, Exchange = "exchange", Route = "route", Queue = "queue")]
public class Updated : UpdateDomainEvent<string> //any type of identity key
{
    //payload
}

//delete event
[MessageBroker(ExchangeType = Exchange.Direct, Exchange = "exchange", Route = "route", Queue = "queue")]
public class Deleted : DeleteDomainEvent<string> //any type of identity key
{
    //payload
}
```

</div>

2 . استفاده از `Event` های تعریف شده در لایه `Domain`

بعد از آنکه `Event` های مورد نیاز در لایه `Domain` ایجاد شدند ، می بایست از این رخداد ها در سطح کلاس های `Entity` استفاده شود . موجودیت های تعریف شده در لایه `Domain` بر پایه الگوی `Rich Domain Model` توسعه پیدا کرده اند و می بایست به ازای هر `Behavior` ای که صدا زده می شود ، در صورت نیاز یک `Event` مناسب ایجاد گردد که برای این مهم می بایست مطابق دستورات زیر عمل نمود .

<div dir="ltr">

```csharp
//update event
[MessageBroker(ExchangeType = Exchange.FanOut, Exchange = "exchange")]
public class UpdatedEvent : UpdateDomainEvent<string> //any type of identity key
{
    public string Email    { get; init; }
    public string Username { get; init; }
}

public class DomainEntity : Entity<string> //any type of identity key
{
    public string Id       { get; private set; }
    public string Email    { get; private set; }
    public string Username { get; private set; }
    
    //Behaviors

    public void Change(string username, string email)
    {
        Email    = email;
        Username = username;

        AddEvent(
            new UpdatedEvent {
                Id       = Id       ,
                Username = username ,
                Email    = email    ,
            }
        );
    }
}
```

</div>

🔥 **توجه** : **تمامی `Entity` های بخش `Command` می بایست از کلاس `<>Entity` ارث بری کنند**

🔥 **توجه** : **برای قدم اول پردازش `Event` های تولیدی در سطح کلاس های `Entity` می بایست در داخل `Behavior` مربوطه در کلاس `Entity` از متد پایه ای `AddEvent` استفاده نمود**

3 . ارسال `Event` های تولید شده در سطح لایه `Domain` به `MessageBroker`
 
بعد از ایجاد و استفاده از `Event` در سطح لایه `Domain` ، حال می بایست نحوه ارسال این رخداد ها به `MessageBroker` و `EventStreamBroker` مورد بررسی قرار گیرد . پردازش `Event` ها در پروژه `Domic` به شکل `OutBox` بوده ، به این صورت که تمامی رخدادها به شکل `Transactional` در پایگاه داده ذخیره می شوند . البته این نکته را باید در نظر گرفت که برای این موضوع حتما می بایست `WithTransactionAttribute` در قسمت `Command` منطق مربوطه ، مورد استفاده قرار بگیرد .

حال برای فعال کردن پردازش `OutBox` تمامی رخدادهای تولید شده در سرویس مربوطه ، می بایست مطابق دستورات زیر عمل نمود .

<div dir="ltr">

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.RegisterEventsPublisher();    //for [ MessageBroker ( RabbitMQ ) ]
builder.RegisterDistributedCaching(); //for [ DistributedLock ] handling
```

</div>

🔥 **توجه** : **در نظر داشته باشید که پردازش `OutBox` رخدادهای تولید شده در سرویس مورد نظر ، به جهت مدیریت `Concurrency` در `Instance` های مختلفی که از سرویس مورد نظر ایجاد می شود ، به ابزار `InternalDistributedCache` نیاز دارد**

🔥 **توجه** : **بازه ی زمانی اجرای مجدد `Job` مورد نیاز برای پردازش `OutBox` رخدادهای ایجاد شده ، `5` ثانیه می باشد**

4 . پردازش و مصرف کردن `Event` های تولید شده

این بخش مهمترین قسمت پیاده سازی شده در پروژه `Domic` می باشد ، زیرا پردازش رخدادهای تولیدی به واسطه سرویس های مختلف ، بسیار موضوع مهم و اصطلاحا `Critical` می باشد که عدم رعایت نکات ریز فنی و دقت به جزئیات ، باعث بروز `Inconsistancy` های مختلف مابین سرویس ها می گردد .

خوشبختانه در پروژه `Domic` به تمامی این موارد و نکات توجه شده است و کاربر نهایی ، صرفا می بایست مطابق دستورات مطرح شده عمل کرده و به راحتی هر چه تمام تر به پردازش این `Event` ها در بستر `MessageBroker` بپردازد .

در ابتدا ، برای پردازش `Event` های تولیدی توسط سرویس های `Producer` ، می بایست کلاس های مربوطه ( `Consumer` ) در لایه `UseCase` ایجاد شوند . برای این مهم مطابق دستورات زیر عمل نمایید .

<div dir="ltr">

```csharp
public class UpdatedConsumerEventBusHandler : IConsumerEventBusHandler<UpdatedEvent>
{
    public UpdatedConsumerEventBusHandler(){}

    [TransactionConfig(Type = TransactionType.Command)] //or => Type = TransactionType.Query
    public void Handle(UpdatedEvent @event)
    {
        //logic
    }
    
    [TransactionConfig(Type = TransactionType.Command)] //or => Type = TransactionType.Query
    public Task HandleAsync(UpdatedEvent @event, CancellationToken cancellationToken)
    {
        //logic
        
        return Task.CompleteTask;
    }
}
```

🔥 **توجه** : **پروژه `Domic` بر پایه الگوی طراحی `CQRS` که یک الگوی `System Design` ایی می باشد ، توسعه پیدا کرده است . لذا در بخش `Consume` کردن `Event` های مربوطه ، حتما باید نوع تراکنش مورد نظر از نظر `Command` و یا `Query` بودن مشخص شود**

🔥 **توجه** : **برای مدیریت تراکنش بخش مربوط به `Query` در مدیریت `Event` و نیز `Message` ، پیش تر در قسمت `Command` های مربوط به الگوی `Mediator` گفته شد که این بخش نیز مشابه آن می باشد منتها با یک تفاوت و آن این است که باید به جای پیاده سازی `ICoreCommandUnitOfWork` ، واسط `ICoreQueryUnitOfWork` پیاده سازی شود**

🔥 **توجه** : **در نظر داشته باشید که در بخش مربوط به مدیریت `Event` ها و یا `Message` ها ، تمام فرآیند به صورت پیشفرض و ثابت ، در یک `Transaction Boundary` صورت می گیرد و صرفا شما به عنوان مدیریت کننده رخداد مربوطه ، باید نوع تراکنش را ( `Command` و یا `Query` ) مشخص نمایید، به این معنی که این رخداد و یا `Message` بر کدام بخش پروژه ( بهتر است بگوییم دیتابیس ) قرار است اثر بگذارد ، دیتابیس `Command` و یا `Query`**

</div>

</div>