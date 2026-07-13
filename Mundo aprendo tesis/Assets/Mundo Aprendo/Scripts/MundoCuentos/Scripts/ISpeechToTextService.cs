using System;

namespace Bolin
{
    // Contrato que permite cambiar el motor de voz sin tocar VoiceRecognitionTest.
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
