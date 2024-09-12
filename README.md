<div dir="rtl" style="font-family: IRANSans">

### 🏆 مقدمه

این پروژه، یک پروژه زیر ساختی برای تمام سرویس های توسعه داده شده بر اساس پروژه `Template` است و شامل ابزارهای کاربردی برای توسعه بهتر و پیشرفته تر سرویس های موجود در معماری میکروسرویس می باشد .

🔥 **توجه** : **تمامی این ابزارها به طور اختصاصی برای این پروژه طراحی و توسعه داده شده اند و دارای نمونه پیاده سازی مشابه نمی باشند .**

<div dir="rtl">

https://github.com/HasanProgrammer/Domic-TemplateService : `Domic-TemplateService`

</div>

برخی از قابلیت ها و امکانات پیاده سازی شده مطابق با استانداردهای روز مهندسی نرم افزار که در این سورس موجود است به شرح زیر می باشد :

-  برخورداری از ابزار مناسب برای مدیریت اصل `CQS` و به طور دقیق تر الگوی `CQRS` با استفاده از ابزار `Mediator` که در اصل این ابزار پیاده کننده الگوی `Mediator` می باشد
- برخورداری از ابزار مناسب برای مدیریت `Distributed Cache` مبتنی بر ابزار زیرساختی `Redis` برای اضافه کردن لایه `Cache` در پروژه
- برخورداری از ابزار های مناسب برای مدیریت الگوی معماری `Event Driven Architecture` که در ادامه به چند مورد از این ابزارها اشاره می شود :
    - برخورداری از ابزار مناسب برای مدیریت بهتر و بهینه تر `Event` و یا `Message` در ساختار پروژه ها مبتنی بر ابزارهای `RabbitMQ` و `Apache Kafka`
    - برخورداری از قابلیت پردازش `OutBox` رخدادهای موجود در سطح سرویس ها با قابلیت اسکیل پذیری ( `Horizontal Scaling` )
    - دارای سامانه مشخص برای دریافت و لاگ تمامی رخدادهای موجود در سرویس ها ( `Snapshot` ) در قالب سرویس مجزا تحت عنوان ( `StateTracker` )
- برخورداری از ابزار لاگ مرکزی یا همان سرویس ( `StateTracker` ) برای مدیریت لاگ خطاهای ایجاد شده در سرویس ها و لاگ رخدادها و یا لاگ های ایجاد شده در سطح کدهای نوشته شده در سرویس ها با ابزار مربوطه ( `Logger` و `StreamLogger` )
- برخورداری از زیرساخت مناسب برای استفاده از ابزارهای `Third Party` استفاده شده به جهت مدیریت لاگ خطاهای سیستمی مانند ابزارهای `ELK` و نیز برخورداری از لاگ مبتنی بر `FileStorage` به طور پیشفرض
- و کلی امکانات و ابزارهای دیگر که توضیح کامل هر کدام از این ابزارها در این مستند به تفصیل بیان خواهد شد

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

---
 
### 🏆 قابلیت های پیشرفته ابزار `Mediator`

1 . استفاده از `WithTransactionAttribute`

از این `Attribute` برای مواقعی که نیاز دارید تا عملیات `Command` خود را در داخل یک `Transaction` مدیریت کنید، استفاده می شود که دارای یک `Property` تحت عنوان `IsolationLevel` می باشد که سطح قفل گزاری منطق شما را در داخل دیتابیس مدیریت می کند ( `Pesemestic Lock` ) .

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

</div>