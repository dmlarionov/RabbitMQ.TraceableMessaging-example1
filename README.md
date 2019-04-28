# What this example is about?

This is demo project for [RabbitMQ.TraceableMessaging](https://github.com/dmlarionov/RabbitMQ.TraceableMessaging) with Application Insights. 

It shows how communications between microservices may be built over RabbitMQ with distributed traceability. You can benefit from scrutinizing this project if you:

1. Need synthetic example of what Application Insights distributed traces can be?
2. Need to learn how to drill down from unsuccessful entry point (API gateway) requests to backend microservice dependency calls and learn finding out root causes of failures.
3. Learn how to implement microservices over message bus with [RabbitMQ.TraceableMessaging](https://github.com/dmlarionov/RabbitMQ.TraceableMessaging).

# Preparation

You'll need Azure with Application Insights instance. Please create it and find instrumentation key at overview page.

You have to install .NET Core SDK 2.2 and (what you like) Visual Studio Code or Visual Studio.

Also, you'll need docker to start (at least) RabbitMQ instance. Or you may start all microservices and API gateway in docker (everything except CLI).

# Build and run

## Approach 1 (Visual Studio Code)

..

## Approach 2 (Visual Studio)

...

## Approach 3 (Everything in Docker)

...

# Exercise

