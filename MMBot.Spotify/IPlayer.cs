namespace MMBot.Spotify
{
    interface IPlayer
    {
        int EnqueueSamples(int channels, int rate, byte[] samples, int frames);
        void Reset();
        void Mute();
        void Unmute();
        void TurnDown(int amount);
        void TurnUp(int amount);
    }
}