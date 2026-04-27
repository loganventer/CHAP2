using CHAP2.Domain.Events;
using CHAP2.Domain.Exceptions;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Domain.Entities;

public class Setlist : IEquatable<Setlist>
{
    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    /// <summary>
    /// True for the per-user auto-saved working setlist that the JS UI
    /// continuously syncs. Hidden from the named list shown to the user.
    /// At most one per owner.
    /// </summary>
    public bool IsWorkingDraft { get; private set; }

    private readonly List<SetlistItem> _items = new();
    public IReadOnlyList<SetlistItem> Items => _items.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Setlist() { }

    internal static Setlist Reconstitute(
        Guid id,
        string ownerId,
        string name,
        DateTime createdAt,
        DateTime? updatedAt,
        bool isWorkingDraft,
        IEnumerable<SetlistItem> items)
    {
        var setlist = new Setlist
        {
            Id = id,
            OwnerId = ownerId,
            Name = name,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            IsWorkingDraft = isWorkingDraft,
        };
        setlist._items.AddRange(items.OrderBy(i => i.Position));
        return setlist;
    }

    public static Setlist Create(string ownerId, string name)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new DomainException("Owner ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Setlist name cannot be empty.");

        var setlist = new Setlist
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        setlist._domainEvents.Add(new SetlistCreatedEvent(setlist.Id, setlist.OwnerId, setlist.Name));
        return setlist;
    }

    public static Setlist CreateWorkingDraft(string ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            throw new DomainException("Owner ID cannot be empty.");

        var setlist = new Setlist
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "(working draft)",
            CreatedAt = DateTime.UtcNow,
            IsWorkingDraft = true,
        };
        setlist._domainEvents.Add(new SetlistCreatedEvent(setlist.Id, setlist.OwnerId, setlist.Name));
        return setlist;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Setlist name cannot be empty.");
        Name = newName.Trim();
        Touch();
    }

    public SetlistItem AppendChorus(Guid chorusId)
    {
        return Append(SetlistItemPayload.ForChorus(chorusId));
    }

    public SetlistItem AppendVerse(string bookId, string bookName, int chapter, int verse, string verseText, string verseRef)
    {
        return Append(SetlistItemPayload.ForVerse(bookId, bookName, chapter, verse, verseText, verseRef));
    }

    private SetlistItem Append(SetlistItemPayload payload)
    {
        var item = SetlistItem.FromPayload(Id, payload, _items.Count);
        _items.Add(item);
        Touch();
        return item;
    }

    public void ReplaceItems(IReadOnlyList<SetlistItemPayload> payloads)
    {
        if (payloads is null) throw new DomainException("Item payloads cannot be null.");

        _items.Clear();
        for (var index = 0; index < payloads.Count; index++)
        {
            _items.Add(SetlistItem.FromPayload(Id, payloads[index], index));
        }
        Touch();
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Setlist item '{itemId}' is not part of setlist '{Id}'.");
        _items.Remove(item);
        Recompact();
        Touch();
    }

    public void Reorder(IReadOnlyList<Guid> itemIdsInOrder)
    {
        if (itemIdsInOrder is null)
            throw new DomainException("Reorder requires the full list of item IDs.");
        if (itemIdsInOrder.Count != _items.Count)
            throw new DomainException("Reorder must reference every existing item exactly once.");

        var byId = _items.ToDictionary(i => i.Id);
        var reordered = new List<SetlistItem>(_items.Count);
        for (var index = 0; index < itemIdsInOrder.Count; index++)
        {
            if (!byId.TryGetValue(itemIdsInOrder[index], out var item))
                throw new DomainException($"Setlist item '{itemIdsInOrder[index]}' is not part of setlist '{Id}'.");
            item.SetPosition(index);
            reordered.Add(item);
        }

        _items.Clear();
        _items.AddRange(reordered);
        Touch();
    }

    public void MarkDeleted()
    {
        _domainEvents.Add(new SetlistDeletedEvent(Id, OwnerId));
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new SetlistUpdatedEvent(Id, OwnerId));
    }

    private void Recompact()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].SetPosition(i);
        }
    }

    public bool Equals(Setlist? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Setlist);

    public override int GetHashCode() => Id.GetHashCode();
}
