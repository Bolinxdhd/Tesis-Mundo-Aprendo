using System;

namespace Bolin
{
    public interface ISpeechToTextService
    {
        event Action<string> OnPartialResult;
        event Action<string> OnFinalResult;
        event Action<string> OnError;
        event Action<string> OnStatusChanged;

        bool IsListening { get; }

        void StartListening();
        void StopListening();
        void DisposeService();
    }
}
