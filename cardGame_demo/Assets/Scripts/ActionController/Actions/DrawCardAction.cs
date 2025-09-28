using System;
public class DrawCardAction : IGameAction
{
    readonly Actor actor;
    readonly PhaseKind phase;

    public DrawCardAction(Actor a, PhaseKind k) { actor = a; phase = k; }

    public void Execute(CombatContext ctx)
    {
        var deck = ctx.GetDeckFor(actor);
        if (deck == null)
        {
            ctx.OnLog?.Invoke($"[Draw] No deck for {actor}. Draw skipped.");
            return;
        }

        var acc = ctx.GetAcc(actor, phase);
        if (acc.IsStanding || acc.IsBusted)
        {
            // Statü sabitse yine de UI’yi güncelle
            ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.GetThreshold(actor, phase));
            return;
        }

        // ==== Relic: bu aksiyondaki çekim adedini relic'lere sor ====
        int drawCount = 1;
        var relics = RelicManager.Instance; // null-safe
        if (relics != null)
            drawCount = Math.Max(0, relics.ApplyDrawCountModifiers(drawCount));

        if (drawCount <= 0)
        {
            ctx.OnLog?.Invoke($"[Draw] Prevented by modifiers for {actor}.");
            ctx.OnProgress?.Invoke(actor, phase, acc.Total, ctx.GetThreshold(actor, phase));
            return;
        }

        int totalBefore = acc.Total;
        int deckBefore = deck.Count;
        int actuallyDrawn = 0;

        for (int i = 0; i < drawCount; i++)
        {
            // Oyuncu bu arada stand/bust olduysa dur.
            if (acc.IsStanding || acc.IsBusted) break;

            if (deck.Count == 0)
            {
                deck.RebuildAndShuffle();
                ctx.OnLog?.Invoke($"[Deck] Empty → Rebuilt+Shuffled for {actor}.");
                // Relic: shuffle kancası
                relics?.OnShuffle();

                if (deck.Count == 0) // hâlâ boşsa çık
                {
                    ctx.OnLog?.Invoke($"[Deck] Still empty after rebuild for {actor}. Stop drawing.");
                    break;
                }
            }

            int threshold = ctx.GetThreshold(actor, phase);
            int before = acc.Total;

            acc.Hit(deck, threshold);
            actuallyDrawn++;

            // Son çekilen kart (varsa)
            var lastCard = acc.Cards.Count > 0 ? acc.Cards[^1] : default;

            // Relic: on card drawn
            relics?.OnCardDrawn(lastCard);

            // UI event
            ctx.OnCardDrawn?.Invoke(actor, phase, lastCard);
            ctx.OnProgress?.Invoke(actor, phase, acc.Total, threshold);

            ctx.OnLog?.Invoke($"[{actor}:{phase}] {before} → {acc.Total} (Deck {deckBefore}->{deck.Count})");

            // Bust/Stand durumu bu çekimde değiştiyse bir sonraki iterasyonda for kırılacak.
        }

        // Özet log
        ctx.OnLog?.Invoke(
            $"[Draw] {actor}:{phase} drew {actuallyDrawn}/{drawCount} (Total {totalBefore}->{acc.Total}, Deck {deckBefore}->{deck.Count})"
        );
    }

    public string Describe() => $"Draw({actor},{phase})";
}
