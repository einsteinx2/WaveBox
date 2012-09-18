using System;
using WaveBox.OperationQueue;

namespace WaveBox
{
    public class FeedCheckOperation : IDelayedOperation
    {
        // property backing ivars
        DelayedOperationState state = DelayedOperationState.None;
        bool isReady = false;
        string operationType = null;

        public DelayedOperationState State 
        { 
            get
            { return state; }
        }
        public DateTime RunDateTime { get; set; }
        public bool IsReady 
        { 
            get { return isReady; }
        }
        public string OperationType { get { return operationType; } }
        
        public void Run()
        {
        }

        public void Cancel()
        {
        }

        public void ResetWait()
        {
        }

        public void Restart()
        {
        }

        public FeedCheckOperation()
        {
        }
    }
}

