Традиционно запросы OData к данным выражаются в виде простых строк без проверки типов при компиляции или поддержки IntelliSense, кроме того, разработчику приходится изучать синтаксис языка запросов. Данная статья описывает библиотеку TsToOdata которая превращает запросы в удобную языковую конструкцию, которые применяется аналогично классам и методам. Вы создаете запросы к строго типизированным коллекциям объектов с помощью ключевых слов языка и знакомых операторов.
<cut />

TsToOdata библиотека для TypeScript явлется подобием LINQ для C#, но в отличии от последнего предназначено только для запросов OData. Для разработчика, который создает запросы, наиболее очевидной частью TsToOdata является интегрированное выражение запроса. Выражения запроса используют декларативный синтаксис запроса. С помощью синтаксиса запроса можно выполнять фильтрацию, упорядочение и группирование данных из источника данных, обходясь минимальным объемом программного кода.

#### Создание модели данных ####  
Первым делом нам надо получить отображение схемы OData на классы TypeScript. 
Первым шагом потребуется сначала получить из EDMX Json схему. Для этого можно воспользоваться библиотекой [OdataToEntity](https://github.com/voronov-maxim/OdataToEntity/wiki/Json-schema).
```cs
IEdmModel edmModel;
using (var reader = XmlReader.Create("edmx_schema.xml"))
    edmModel = CsdlReader.Parse(reader);

var generator = new OeJsonSchemaGenerator(edmModel);
using (var utf8Json = new MemoryStream())
{
    generator.Generate(utf8Json);
    utf8Json.Position = 0;
    File.WriteAllBytes("json_schema.json", utf8Json.ToArray());
}
```
Вторым ашгом из Json схемы мы можем уже получить модель данных на TypeScript. Для этого можно воспользоваться библиотекой [quicktype](https://github.com/quicktype/quicktype).
В результате у меня получилась [схема данных](https://raw.githubusercontent.com/voronov-maxim/TsToOdata/master/test/order.ts) которую я буду использовать в дальнейших примерах.

#### Установка TsToOdata ####  
```
npm install ts2odata
```

#### Создание контекста доступа к данным ####  
```javascript
import { EntitySet, OdataContext } from 'ts2odata';
import * as oe from './order';

export class OrderContext extends OdataContext<OrderContext> {
    Categories = EntitySet.default<oe.Category>();
    Customers = EntitySet.default<oe.Customer>();
    OrderItems = EntitySet.default<oe.OrderItem>();
    OrderItemsView = EntitySet.default<oe.OrderItemsView>();
    Orders = EntitySet.default<oe.Order>();
}
```
```javascript
let context: OrderContext = OdataContext.create(OrderContext, 'http://localhost:5000/api');
```

#### Примеры запросов ####  
Получить все записи в таблицы
```javascript
context.Orders;
//http://localhost:5000/api/Orders
```
Получить выбранные столбцы
```javascript
context.Orders.select(o => { return { p: o.Name } });
//http://localhost:5000/api/Orders?$select=Name
```
Сортировка по возрастанию
```javascript
context.Orders.orderby(i => i.Id);
//http://localhost:5000/api/Orders?$orderby=Id
```
Сортировка по убыванию
```javascript
context.Orders.orderbyDescending(i => i.Id);
//http://localhost:5000/api/Orders?$orderby=Id desc
```
Фильтрация
```javascript
context.Orders.filter(o => o.Date.getFullYear() == 2016);
//http://localhost:5000/api/Orders?$filter=year(Date) eq 2016
```
Получить связанные данные
```javascript
context.Orders.expand(o => o.Items);
//http://localhost:5000/api/Orders?$expand=Items
```
Получить связанные данные на несколько вложенных уровней
```javascript
context.Customers..expand(c => c.Orders).thenExpand(o => o.Items);
//http://localhost:5000/api/Customers?$expand=Orders($expand=Items)
```
Пропустить несколько записей
```javascript
context.Orders.orderby(i => i.Id).skip(2);
//http://localhost:5000/api/Orders?$orderby=Id&$skip=2
```
Взять несколько записей
```javascript
context.Orders.orderby(i => i.Id).top(3);
//http://localhost:5000/api/Orders?$orderby=Id&$top=3
```
Группировка
```javascript
context.OrderItems.groupby(i => { return { Product: i.Product } });
//localhost:5000/api/OrderItems?$apply=groupby((Product))
```
Агрегация
```javascript
context.OrderItems.groupby(i => { return { OrderId: i.OrderId, Status: i.Order.Status } })
    .select(g => {
        return {
            orderId: g.key.OrderId,
            avg: g.average(i => i.Price),
            dcnt: g.countdistinct(i => i.Product),
            max: g.max(i => i.Price),
            max_status: g.max(_ => g.key.Status),
            min: g.min(i => i.Price),
            sum: g.sum(i => i.Price),
            cnt: g.count()
        }});
//http://localhost:5000/api/OrderItems?$apply=groupby((OrderId,Order/Status),aggregate(Price with average as avg,Product with countdistinct as dcnt,Price with max as max,Order/Status with max as max_status,Price with min as min,Price with sum as sum,$count as cnt))
```
Выборка по ключу
```javascript
context.Customers.key({ Country: 'RU', Id: 1 });
//http://localhost:5000/api/Customers(Country='RU',Id=1)
```
Выборка по ключу и навигационному свойству
```javascript
context.OrderItems.key(1, i => i.Order.Customer);
//http://localhost:5000/api/OrderItems(1)/Order/Customer
```
Вычесляемые столбцы
```javascript
context.OrderItems
    .select(i => {
        return {
            product: i.Product,
            Total: i.Count * i.Price,
            SumId: i.Id + i.OrderId
        }
    });
//http://localhost:5000/api/OrderItems?$select=Product&$compute=Count mul Price as Total,Id add OrderId as SumId
```
Лямбда операторы
```javascript
context.Orders.filter(o => o.Items.every(i => i.Price >= 2.1));
//http://localhost:5000/api/Orders?$filter=Items/all(d:d/Price ge 2.1)
```
```javascript
context.Orders.filter(o => o.Items.some(i => i.Count > 2));
//http://localhost:5000/api/Orders?$filter=Items/any(d:d/Count gt 2)
```
IN оператор
```javascript
let items = [1.1, 1.2, 1.3];
context.OrderItems.filter(i => items.includes(i.Price), { items: items });
//http://localhost:5000/api/OrderItems?$filter=Price in (1.1,1.2,1.3)
```
Количество записей
```javascript
context.Orders.count();
//http://localhost:5000/api/Orders/$count
```
Вернуть контекст источника данных
Метод *asEntitySet* необходим когда надо выполнить сортировку по столбцам отсутствующим в выборке
```javascript
context.Orders(o => o.AltCustomer).thenSelect(o => {{
    p1: o.Address,
    : o.Country,
    : o.Id,
    : o.Name,
    : o.Sex
}}).asEntitySet().orderby(o => o.Id)
//http://localhost:5000/api/Orders?$expand=AltCustomer($select=Address,Country,Id,Name,Sex)&$orderby=Id
```
Остальные примеры можно посмотреть на [GitHub](https://github.com/voronov-maxim/TsToOdata/blob/master/test/QueryTests.ts).

Следует заметить что методы *select*, *expand*, *groupby* изменяют контекст, их результатом становиться новый тип и что бы продолжить выполнение в этом новом контексте нужно использовать методы с приставкой *then*: *thenFilter*, *thenExpand*, *thenSelect*, *thenOrderby*, *thenOrderbyDescending*, *thenSkip*, *thenTop*. Методы *select* и *thenSelect* необратимо меняют контекст и чтобы вернуться к родительскому контексту надо применить метод *asEntitySet*.

#### Параметризация запросов ####  
Запросы фильтрации - *filter*, выборки - *select*, групировки - *groupby* можно параметризировать, имена свойств объекта параметризации должны совпадать с именами переменных в коде запроса.
```javascript
let count: number | null = null;
context.OrderItems.filter(i => i.Count == count, { count: count }); //http://localhost:5000/api/OrderItems?$filter=Count eq null
```
```javascript
let s = {
    altCustomerId: 3,
    customerId: 4,
    dateYear: 2016,
    dateMonth: 11,
    dateDay: 20,
    date: null,
    name: 'unknown',
    status: "OdataToEntity.Test.Model.OrderStatus'Unknown'",
    count1: 0,
    count2: null,
    price1: 0,
    price2: null,
    product1: 'unknown',
    product2: 'null',
    orderId: -1,
    id: 1
};
context.Orders.filter(o => o.AltCustomerId == s.altCustomerId && o.CustomerId == s.customerId && (o.Date.getFullYear() == s.dateYear && o.Date.getMonth() > s.dateMonth && o.Date.getDay() < s.dateDay || o.Date == s.date) && o.Name.includes(s.name) && o.Status == s.status, s).expand(o => o.Items).thenFilter(i => (i.Count == s.count1 || i.Count == s.count2) && (i.Price == s.price1 || i.Price == s.price2) && (i.Product.includes(s.product1) || i.Product.includes(s.product2)) && i.OrderId > s.orderId && i.Id != s.id, s);
//http://localhost:5000/api/Orders?$filter=AltCustomerId eq 3 and CustomerId eq 4 and (year(Date) eq 2016 and month(Date) gt 11 and day(Date) lt 20 or Date eq null) and contains(Name,'unknown') and Status eq OdataToEntity.Test.Model.OrderStatus'Unknown'&$expand=Items($filter=(Count eq 0 or Count eq null) and (Price eq 0 or Price eq null) and (contains(Product,'unknown') or contains(Product,'null')) and OrderId gt -1 and Id ne 1)
```
#### Отображение функций ####  
| JavaScript    |  OData     |
|---------------|------------|
| Math.ceil      | ceiling     |
| concat          | concat    |
| includes       | contains  |
| getDay         | day         |
| endsWith     | endswith  |
| Math.floor    | floor        |
| getHours      | hour       |
| indexOf        | indexof    |
| stringLength | length     |
| getMinutes   | minute    |
| getMonth     | month     |
| Math.round  | round      |
| getSeconds  | second     |
| startsWith    | startswith |
| substring     | substring  |
| toLowerCase | tolower   |
| toUpperCase | toupper   |
| trim             | trim        |
| getFullYear   | year        |
Для длины строки нужно использовать *OdataFunctions.stringLength*
```javascript
context.Customers.filter(c => OdataFunctions.stringLength(c.Name) == 5); //http://localhost:5000/api/Customers?$filter=length(Name) eq 5
```
Для длины массива нужно использовать *OdataFunctions.arrayLength*
```javascript
context.Orders.filter(o => OdataFunctions.arrayLength(o.Items) > 2); //http://localhost:5000/api/Customers?$filter=Items/$count gt 2
```

#### Получение результатов ####  
Методы описывающие запрос такие как select, filter и другие должны закачиваться методом *getQueryUrl* или *toArrayAsync*.
*getQueryUrl* возвращает URL запроса. Результатом этого кода на TypeScript:
```javascript
let url: URL = context.Customers
    .expand(c => c.AltOrders).thenExpand(o => o.Items).thenOrderby(i => i.Price)
    .expand(c => c.AltOrders).thenExpand(o => o.ShippingAddresses).thenOrderby(s => s.Id)
    .expand(c => c.Orders).thenExpand(o => o.Items).thenOrderby(i => i.Price)
    .expand(c => c.Orders).thenExpand(o => o.ShippingAddresses).thenOrderby(s => s.Id)
    .orderby(c => c.Country).orderby(c => c.Id).getQueryUrl();
```
будет OData запрос:
```
http://localhost:5000/api/Customers?$expand=
AltOrders($expand=Items($orderby=Price),ShippingAddresses($orderby=Id)),
Orders($expand=Items($orderby=Price),ShippingAddresses($orderby=Id))
&$orderby=Country,Id
```
*toArrayAsync* возвращает результат запроса в виде Json. Результатом этого кода на TypeScript:
```javascript
context.Customers
    .expand(c => c.Orders).thenSelect(o => { return { Date: o.Date } }).orderby(o => o.Date)
    .asEntitySet().select(c => { return { Name: c.Name } }).orderby(c => c.Name).toArrayAsync();
```
будет Json:
```json
[{
		"Name": "Ivan",
		"Orders": [{
				"Date": "2016-07-04T19:10:10.8237573+03:00"
			}, {
				"Date": "2020-02-20T20:20:20.000002+03:00"
			}
		]
	}, {
		"Name": "Natasha",
		"Orders": [{
				"Date": "2016-07-04T19:10:11+03:00"
			}
		]
	}, {
		"Name": "Sasha",
		"Orders": []
	}, {
		"Name": "Unknown",
		"Orders": [{
				"Date": null
			}
		]
	}
]
```
Если нужно получить свойство как дату, а не строку можно вызвать *toArrayAsync* с необязательным параметром  *OdataParser*:
```javascript
import { OdataParser } from 'ts2odata';
import schema from './schema.json';

let odataParser = new OdataParser(schema);
context.Orders.toArrayAsync(odataParser);
```

#### Типы перечислений (enum) ####  
Если ваш OData сервис не поддерживает перечисления без пространства имен (Namespace), для правильной трансляции необходимо передать его значение в метод создания контекста данных:
```javascript
let context: OrderContext = OdataContext.create(OrderContext, 'http://localhost:5000/api', 'OdataToEntity.Test.Model');
```
В некоторых случаях для правильной трансляции перечислений может потребоваться создание объекта *OdataParser*.
```javascript
import { OdataParser } from 'ts2odata';
import schema from './schema.json';

let odataParser = new OdataParser(schema);
let context: OrderContext = OdataContext.create(OrderContext, 'http://localhost:5000/api', 'OdataToEntity.Test.Model', odataParser);
```

#### Исходный код ####  
Исходный код лежит на [GitHub](https://github.com/voronov-maxim/TsToOdata).
В папке source - код Node пакета, в папке test - тесты.