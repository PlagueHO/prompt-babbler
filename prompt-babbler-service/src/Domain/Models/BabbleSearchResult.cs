namespace PromptBabbler.Domain.Models;

public sealed record BabbleSearchResult(Babble Babble, double SimilarityScore);
