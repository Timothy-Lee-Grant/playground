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

public class StateService : . 
{
    private IDictionary<> _pubSub = new Dictionary<string, List<>>();
    public StateService()
    {
        // What will I want in here?
        // I know that I will want a way to communicate with my Woker.
        // Does this mean that I will want some kind of pointer (callback) from that item?
        // It is registered as a hosted service, so it will continually run that ExecuteAsync infinate while loop

        // I will need a registry which I can know all of the things which have subscribed.
        // but if we have multiple different publishers, then we will need multiple different lists for each of the subscribers.
        // So we will need a dictionary.

    }

    // I would guess that this would need to return back a function pointer. but I also know that c# does not like to do that
    public something SubscribeToMyEvents()
    {
        
    }
}