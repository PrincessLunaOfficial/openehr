namespace OpenEhr;

/// <summary>The current lifecycle state of a versioned composition.</summary>
public sealed record CompositionStatus(Guid EhrId, Guid VoId, string Cuid, string State);
