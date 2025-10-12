using System;

public interface IMystery
{
    void Init(MysteryContext ctx);
    event Action<MysteryResult> OnMysteryCompleted;
}
