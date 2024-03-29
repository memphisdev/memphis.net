﻿namespace Memphis.Client.Core;

public class MemphisMessageHandlerEventArgs : EventArgs
{
    public MemphisMessageHandlerEventArgs(List<MemphisMessage> messageList, IJetStream? context, System.Exception? ex)
    {
        MessageList = messageList;
        Context = context;
        Exception = ex;
    }

    public MemphisMessageHandlerEventArgs(List<MemphisMessage> messageList, IConsumerContext? context, System.Exception? ex): this(messageList, default(IJetStream), ex)
    {
        ConsumerContext = context;
    }


    /// <summary>
    /// Retrieves the message.
    /// </summary>
    public List<MemphisMessage> MessageList { get; }

    public System.Exception? Exception { get; }

    public IJetStream? Context { get; set; }

    public IConsumerContext? ConsumerContext { get; set; }
}