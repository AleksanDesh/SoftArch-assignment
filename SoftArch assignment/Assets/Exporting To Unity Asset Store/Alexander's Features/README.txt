This is my exportable package, check the example scene for an example.

To add new quests, first:

In the NormalQuest you should add the type of quest (if appropriate) in the Enum:
    public enum ObjectiveType
    {
        CollectItem,
        DefeatEnemy,
        ReachLocation,
        TalkNPC,
        Custom
    }

Then add a subscription to the according event type inside Quest Progress, and in UnsubscribeFromAll() method:
                switch (obj.type)
                { // *Event bus checks if duplicate*
                    case ObjectiveType.CollectItem:
                        if (QuestBus.Instance != null) QuestBus.Instance.Subscribe<ItemPickedEvent>(TryIncrement);
                        break;
                    case ObjectiveType.DefeatEnemy:
                        if (QuestBus.Instance != null) QuestBus.Instance.Subscribe<DeathEvent>(TryIncrement);
                        break;
                    case ObjectiveType.ReachLocation:
                        break;
                    case ObjectiveType.TalkNPC:
                        break;
                    case ObjectiveType.Custom:
                        break;
                    default:
                        break;
                }

UnsubscribeFromAll():
                switch (obj.type)
                {
                    case ObjectiveType.CollectItem:
                        if (QuestBus.Instance != null) QuestBus.Instance.Unsubscribe<ItemPickedEvent>(TryIncrement);
                        break;
                    case ObjectiveType.DefeatEnemy:
                        if (QuestBus.Instance != null) QuestBus.Instance.Unsubscribe<DeathEvent>(TryIncrement);
                        break;
                    case ObjectiveType.ReachLocation:
                        break;
                    case ObjectiveType.TalkNPC:
                        break;
                    case ObjectiveType.Custom:
                        break;
                    default:
                        break;
                }

If you want a custom event, then go to BaseEvent, and add any events you wish.

If you have any questions, please reach out to me via email: deralehander@gmail.com