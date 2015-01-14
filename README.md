***note - this is a work in progress so updates, documentation will be going up in fits and starts***

# EntityMap

EntityMap is a library that starts with the database then works backward. In some ways it represents an evolution of thought that has went full circle and arrived back at "it's just data...why are we complicating this?". There are good and compelling reasons for object orientation however I've become increasingly uneasy about its use large business applications where ultimately the aim is to manage data. 

There is nothing particularly radical about EntityMap's approach to managing data. In fact it's been heavily influenced by working with applications like Microsoft CRM and when I really think about it, takes me back to my days at college using Oracle Forms. 

The nature of business means that business applications tend to be bloated. If it exists as a concept within the business you'll generally have a table in a database somewhere that represents it, Account, Customer, Invoice, Order, Contact, Address, Lead, Product...etc. The problem for the software developer is that business data and business logic are generally the product of many years of evolution within the business that many hands helped mould and shape. The result of which leads to a fundamental truth that every software developer should accept if they value their sanity - business data is not clean.

Once you accept this truth and the subsequent truth that the business isn't about to tear up its long established data and processes so you can write nice clean object hierarchies against a beautifully organised, third normal form normalised database the happier you'll be. Now this isn't to say you shouldn't do everything in your power to organise and streamline data, to eliminate duplication of data and inefficiencies where you find them, just be prepared to be told "NO" when it threatens to interfere with some long established process the business holds dear.

So the primary goal of EntityMap is to treat data as data. You DON'T need an object for every table in your database. If you do create an object for every table (or rather business concept) you'll soon end up with a sprawling, unmanageable domain model backed by an equally sprawling and unmanageable ORM layer.

##How does it work?##

EntityMap treats every database table as an "entity", it can inspect a database table and build an entity template which in effect is simply a collection of properties. The initial concept behind EntityMap was "here's a dictionary, throw it into the database".

###Add###

In the example below you'll see __CreateEntity__ being called, this is what inspects the table __Person__ (first looking to see if the table has been cached) and returns an empty entity template (looking to change this name going forward as it could lead to confusion).

__SetValue__ is then called on the entity similar to adding to a dictionary. Internally the entity is aware of every property and type so if you attempted to set the __DOB__ property as a string you'd receive an exception to that effect.

__Create__ is then called which generates the sql needed to insert the data into the database.

```csharp
using (IEntitySession session = new SqlEntitySession(connectionString)) {
     Entity personEntity = session.CreateEntity("Person");

     personEntity.SetValue("Id", Guid.NewGuid());
     personEntity.SetValue("FirstName", "Joe");
     personEntity.SetValue("LastName", "Blogs");
     personEntity.SetValue("DOB", DateTime.Parse("1980-01-01"));

     session.Create(personEntity);
}
```

###Retrieve###
Under the hood EntityMap uses query objects to retrieve records allowing developers to write their own should they need customised queries. Built in are two standard queries for retrieving a single record by "Id" and retrieving multiple records with pagination and ordering.

####QuerySingle####

```csharp
using (IEntitySession session = new SqlEntitySession(connectionString)) {
     Entity person = session.Retrieve("Person", new Guid(" ... "));
}
```

####QueryMultiple####

```csharp
using (IEntitySession session = new SqlEntitySession(connectionString)) {
     IList<OrderExpression> orderExpressions = new List<OrderExpression>();
     orderExpressions.Add(new OrderExpression("FirstName", OrderType.ASC));
     
     List<Entity> entityCollection = session.RetrieveMultiple("Person", 1, 50, orderExpressions).ToList();
}
```

The __pageNumber__ parameter starts at 1 rather than a zero based index. At the moment you must specify at least one __OrderExpression__ value, I'll be looking to clean up this syntax at a later date.

####Custom Query####

For custom queries a developer can implement their own query object by implementing the IQuery interface and extending the QueryBase class. By implementing the __GetQuery__ method you only need to return the SQL to be used in the query, the base class handles the rest  :-

```csharp
public class CustomQuery : QueryBase, IQuery {
     protected override string GetQuery(Entity entity) {
          return "SELECT * FROM Person WHERE FirstName LIKE 'J%'";
     }
}
```

Then to use this query object :-

```csharp
using (IEntitySession session = new SqlEntitySession(connectionString)) {
     CustomQuery customQuery = new CustomQuery();
     List<Entity> entityCollection = customQuery.GetResult(session).ToList();
}
```
