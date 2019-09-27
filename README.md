# dotnetcore-nats-play
NATS Playground

In the solution folder run the following command
```
docker-compose -f .\docker-compose.yml up --scale nats-replier=2 --scale nats-subscribe=2
```

nats-replier is listening to the "dog" subject and is in a queue group, so the replier will be randomly selected to process the message.

```
...src\Requestor>dotnet run -verbose -subject=dog -count 50
```


To verify that it is up navigate [http://localhost:8222/](http://localhost:8222/)  

The examples, which I copied from [nats.net](https://github.com/nats-io/nats.net) assume the standard ports (4222) which you can override.  

