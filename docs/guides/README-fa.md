# راهنمای‌های Raziee.SharedKernel

این دایرکتوری شامل راهنمای‌های جامع برای پیاده‌سازی الگوها و شیوه‌های معماری مختلف با استفاده از Raziee.SharedKernel است.

## راهنمای‌های موجود

### 1. [راهنمای Domain-Driven Design (DDD)](./ddd-guide-fa.md)
یادگیری نحوه پیاده‌سازی الگوهای Domain-Driven Design با Raziee.SharedKernel:
- Entities و Value Objects
- Aggregates و Domain Events
- Domain Services
- الگوی Repository
- مثال کامل سیستم تجارت الکترونیک

### 2. [راهنمای Modular Monolith](./modular-monolith-guide-fa.md)
ساخت modular monolith هایی که می‌توانند به microservices تکامل یابند:
- اصول طراحی Module
- ارتباط Module ها
- Integration Events
- Migration به Microservices
- مثال کامل پلتفرم تجارت الکترونیک

### 3. [راهنمای Microservices](./microservices-guide-fa.md)
پیاده‌سازی معماری‌های microservices با الگوهای ارتباط مناسب:
- ارتباط سرویس‌ها
- یکپارچه‌سازی Message Bus
- الگوهای سازگاری داده
- Circuit Breaker و سیاست‌های Retry
- Service Discovery
- مثال کامل Microservices تجارت الکترونیک

### 4. [راهنمای الگوی Repository](./repository-pattern-guide-fa.md)
تسلط بر الگوی Repository با Raziee.SharedKernel:
- پیاده‌سازی Generic Repository
- الگوی Specification
- الگوی Unit of Work
- ویژگی‌های پیشرفته Repository
- مثال کامل سیستم تجارت الکترونیک

### 5. [راهنمای CQRS](./cqrs-guide-fa.md)
پیاده‌سازی Command Query Responsibility Segregation:
- پیاده‌سازی Command و Query
- Pipeline Behaviors
- Validation و Logging
- مدیریت Cache و Transaction
- مثال کامل سیستم تجارت الکترونیک

### 6. [راهنمای Multi-Tenancy](./multitenancy-guide-fa.md)
ساخت برنامه‌های multi-tenant با جداسازی مناسب داده:
- الگوهای Multi-Tenancy
- شناسایی Tenant
- جداسازی داده
- Tenant-Aware Entities
- فیلتر کردن Query
- مثال کامل برنامه SaaS

### 7. [راهنمای Distributed Transactions](./distributed-transactions-guide-fa.md)
پیاده‌سازی distributed transactions با استفاده از الگوی Saga:
- اصول الگوی Saga
- Saga Orchestrator
- پیاده‌سازی Saga Steps
- مدیریت خطا و Compensation
- مثال کامل پردازش سفارش تجارت الکترونیک

### 8. [راهنمای الگوهای Messaging](./messaging-patterns-guide-fa.md)
پیاده‌سازی الگوهای messaging قابل اعتماد برای ارتباط غیرهمزمان:
- پیاده‌سازی Message Bus
- الگوی Inbox/Outbox
- Message Consumer/Publisher
- مثال کامل Messaging تجارت الکترونیک

### 9. [راهنمای ارتباط سرویس‌ها](./service-communication-guide-fa.md)
ساخت ارتباط سرویس مقاوم با circuit breakers و سیاست‌های retry:
- الگوی Circuit Breaker
- سیاست‌های Retry
- Service Discovery
- مثال کامل ارتباط سرویس مقاوم

### 10. [راهنمای Entity Framework Extensions](./entity-framework-extensions-guide-fa.md)
استفاده از Entity Framework extensions برای پیکربندی دیتابیس و فیلدهای audit:
- ModelBuilder Extensions
- Auditable Entity Interceptor
- DbContext Base
- مثال کامل دیتابیس تجارت الکترونیک

### 11. [راهنمای معماری Vertical Slice](./vertical-slice-architecture-guide-fa.md)
پیاده‌سازی معماری Vertical Slice با رابط IFeature:
- اصول معماری Vertical Slice
- رابط IFeature
- کلاس FeatureBase
- مدیریت Features
- مثال کامل برنامه تجارت الکترونیک

## شروع سریع

هر راهنما شامل موارد زیر است:
- **مقدمه** - نمای کلی از الگو و مزایای آن
- **اصول** - مفاهیم و اصول اصلی
- **پیاده‌سازی** - مثال‌های پیاده‌سازی گام به گام
- **مثال‌های کامل** - مثال‌های برنامه‌های دنیای واقعی
- **بهترین شیوه‌ها** - شیوه‌ها و الگوهای توصیه شده

## شروع کار

1. راهنمایی را انتخاب کنید که با نیازهای معماری شما مطابقت دارد
2. مثال‌های پیاده‌سازی گام به گام را دنبال کنید
3. مثال‌های کامل را برای سناریوهای دنیای واقعی مطالعه کنید
4. بهترین شیوه‌ها را در پروژه‌های خود اعمال کنید

## مشارکت

اگر می‌خواهید در این راهنماها مشارکت کنید یا راهنماهای جدید پیشنهاد دهید، لطفاً:
1. repository را fork کنید
2. راهنمای جدید ایجاد کنید یا راهنماهای موجود را بهبود دهید
3. pull request ارسال کنید

## پشتیبانی

برای سوالات یا پشتیبانی در مورد این راهنماها:
- issue در repository ایجاد کنید
- مستندات اصلی را بررسی کنید
- مثال‌ها را در دایرکتوری `docs/examples` مرور کنید
