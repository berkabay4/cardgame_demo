public class DrawCardAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public DrawCardAction(Actor a, PhaseKind k){ actor=a; phase=k; }

    public void Execute(CombatContext ctx)
    {
        var deck = ctx.GetDeckFor(actor);
        if (deck == null)
        {
            ctx.OnLog?.Invoke($"[Draw] No deck for {actor}. Draw skipped.");
            return;
        }

        var acc = ctx.GetAcc(actor, phase);

        // Eğer bu faz zaten Stand/Bust durumundaysa kart çekmeyelim
        if (acc.IsStanding || acc.IsBusted)
        {
            ctx.OnLog?.Invoke($"[Draw] {actor}:{phase} is already {(acc.IsStanding ? "STANDING" : "BUSTED")} → no draw.");
            // UI senkron kalsın diye mevcut durumu yayınlayalım
            ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.Threshold);
            return;
        }

        // Boşsa hemen rebuild + aynı çağrıda çek
        if (deck.Count == 0)
        {
            deck.RebuildAndShuffle();
            ctx.OnLog?.Invoke($"[Deck] Empty → Rebuilt+Shuffled for {actor}. Drawing now.");
            if (deck.Count == 0)
            {
                ctx.OnLog?.Invoke($"[Deck] Still empty after rebuild for {actor} — aborting draw.");
                return;
            }
        }

        int before = acc.Total;
        int beforeDeck = deck.Count;

        acc.Hit(deck, ctx.Threshold); // kart çekmeyi her zaman Hit yapıyor

        var lastCard = acc.Cards.Count > 0 ? acc.Cards[^1] : default;
        ctx.OnCardDrawn?.Invoke(actor, phase, lastCard);
        ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.Threshold);
        ctx.OnLog?.Invoke($"[{actor}:{phase}] {before} → {acc.Total} (Deck {beforeDeck}->{deck.Count})");
    }

    public string Describe()=> $"Draw({actor},{phase})";
}
