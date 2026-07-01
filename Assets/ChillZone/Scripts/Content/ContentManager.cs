using System;
using System.Collections.Generic;
using System.Linq;
using ChillZone.Core;
using ChillZone.Player;
using UnityEngine;

namespace ChillZone.Content
{
    /// <summary>
    /// Scene-local singleton that owns content <em>selection</em> (which item is active) and its
    /// persistence for every content family — balls, baskets, and any future type — from one place.
    /// Each wired <see cref="UnlockableContentRegistry"/> declares its own <see cref="ContentTypes"/>,
    /// so selection is keyed by type with no per-type code here. Spawning and placement live elsewhere
    /// (BallSpawnManager, BasketSpawnManager); this just answers "which item is selected" and persists it.
    /// Not DontDestroyOnLoad — a fresh instance is created each scene load; the persisted pick is read
    /// from the player profile (with a PlayerPrefs mirror as a fallback).
    /// </summary>
    public class ContentManager : MonoBehaviour
    {
        public static ContentManager Instance { get; private set; }

        [Serializable]
        private class ContentSource
        {
            [Tooltip("Registry for one content type. The registry itself declares its ContentType.")]
            public UnlockableContentRegistry registry;
            [Tooltip("Optional explicit default; falls back to the registry's first item.")]
            public UnlockableContent defaultContent;
        }

        [SerializeField, Tooltip("One entry per content type (ball, basket, …). Each registry declares its own ContentType.")]
        private List<ContentSource> sources = new();

        private readonly Dictionary<ContentTypes, ContentSource> _sourcesByType = new();
        private readonly Dictionary<ContentTypes, UnlockableContent> _selected = new();

        #region lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            BuildSourceMap();
            foreach (var type in _sourcesByType.Keys)
                _selected[type] = LoadSelection(type);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region selection

        /// <summary>The selected content for a type, loading the persisted pick on first access. Null if that type isn't wired.</summary>
        public UnlockableContent GetSelected(ContentTypes type)
        {
            if (_selected.TryGetValue(type, out var selected) && selected) return selected;
            selected = LoadSelection(type);
            _selected[type] = selected;
            return selected;
        }

        /// <summary>Typed convenience for <see cref="GetSelected(ContentTypes)"/>, e.g. GetSelected&lt;BallData&gt;(ContentTypes.Ball).</summary>
        public T GetSelected<T>(ContentTypes type) where T : UnlockableContent => GetSelected(type) as T;

        /// <summary>Select an item; routes by its own <see cref="UnlockableContent.ContentType"/> and persists the choice.</summary>
        public void Select(UnlockableContent content)
        {
            if (!content) return;
            var type = content.ContentType;
            _selected[type] = content;
            PlayerPrefs.SetString(PrefKey(type), content.GetStableId());
            PlayerProfileManager.Instance?.SelectContent(type, content.GetStableId());
        }

        /// <summary>Select the item with the given id from a type's registry (no-op if the type isn't wired or the id is unknown).</summary>
        public void SelectById(ContentTypes type, string id)
        {
            if (!_sourcesByType.TryGetValue(type, out var source) || source.registry == null) return;
            var content = source.registry.GetById(id);
            if (content != null) Select(content);
        }

        #endregion

        #region internals

        private void BuildSourceMap()
        {
            _sourcesByType.Clear();
            foreach (var source in sources.Where(source => source?.registry != null))
            {
                _sourcesByType[source.registry.contentType] = source;
            }
        }

        // Load the persisted selection for a type: the profile's stored id (falling back to the PlayerPrefs
        // mirror), resolved against the registry; then the explicit default, then the registry's first item.
        private UnlockableContent LoadSelection(ContentTypes type)
        {
            if (!_sourcesByType.TryGetValue(type, out var source) || !source.registry) return null;

            var savedId = PlayerPrefs.GetString(PrefKey(type), "");
            if (PlayerProfileManager.Instance)
            {
                var profileId = PlayerProfileManager.Instance.EnsureProfile().GetSelectedContentId(type);
                if (!string.IsNullOrWhiteSpace(profileId)) savedId = profileId;
            }

            var content = source.registry.GetById(savedId);
            if (!content) content = source.defaultContent ? source.defaultContent : source.registry.GetDefaultContent();
            return content;
        }

        // PlayerPrefs mirror key. Reuses the existing PrefKeys constants for known types so saves stay
        // compatible; a future type falls back to the same "selected_<type>" convention those constants use.
        private static string PrefKey(ContentTypes type) => type switch
        {
            ContentTypes.Ball => PrefKeys.SelectedBall,
            ContentTypes.Basket => PrefKeys.SelectedBasket,
            _ => "selected_" + type.ToString().ToLowerInvariant()
        };

        #endregion
    }
}
