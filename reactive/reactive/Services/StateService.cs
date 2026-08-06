/*
StateService will be the owner of all state changes. 
No other entity is allowed to publish events.
Other classes are only allowed to observe those published events.

We should expose the event as an observable. What does that mean? 
So if I have my worker service which is 'doing events' and I am here in this file,
then I guess I need a way to send out those events to all of the listening (subscribed)
callback? 

So how can I think to do this? 
*/

public class StateService
{
    // After reading the documentation for lecture 001-The-Broadcaster
    // Now I will try to redo this 

    // First I will use the delegate.
    // The documentation said that there were multiple layers of abstraction. delegates, IObservables, and Rx.NET 

    public delegate void OnEventTrigger(string message);

    // Now I know that I have a .Invoke() method available to me on this delegate 
    // but how do I get all of this connected from when my worker (is this in the future iterations called the observer), wants to notify me (the broadcaster) that a trigger has occured?

    // I know that it is possible to do += now on my delegate and now when I do invoke() it will do the invocation of all the subscribed, but how does the worker tell me?

    private void Publish()
    {
        OnEventTrigger?.Invoke();
    }
}