using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FolderAssetImporter
{
    [Serializable]
    internal class FolderAssetImportSetting
    {
        [SerializeField] private Object _holder;
        [SerializeField] private bool _enableAssetPresetting;
        [SerializeField] private List<AssetPresettingRule> _assetPresettingRules = new();
        [SerializeField] private bool _enableAddressNaming;
        [SerializeField] private List<AddressNamingRule> _addressNamingRules = new();

        internal bool EnableAssetPresetting => _enableAssetPresetting;
        internal bool EnableAddressNaming => _enableAddressNaming;

        internal Object Holder
        {
            get => _holder;
            set => _holder = value;
        }

        internal bool IsValid()
        {
            return _holder != null && (_enableAssetPresetting || _enableAddressNaming ||
                                       _assetPresettingRules.Count > 0 || _addressNamingRules.Count > 0);
        }

        internal void Clear()
        {
            _enableAssetPresetting = false;
            _enableAddressNaming = false;
            _assetPresettingRules.Clear();
            _addressNamingRules.Clear();
        }

        internal bool CollectAppliers(string assetPath, List<AssetPresettingRule.Applier> appliers)
        {
            if (!_enableAssetPresetting) return false;
            foreach (var rule in _assetPresettingRules)
            {
                if (rule.TryGetApplier(assetPath, Holder, out var applier))
                {
                    appliers.Add(applier);
                }
            }

            return appliers.Count > 0;
        }

        internal bool CollectAppliers(string assetPath, List<AddressNamingRule.Applier> appliers)
        {
            if (!_enableAddressNaming) return false;
            foreach (var rule in _addressNamingRules)
            {
                if (rule.TryGetApplier(assetPath, Holder, out var applier))
                {
                    appliers.Add(applier);
                }
            }

            return appliers.Count > 0;
        }
    }
}