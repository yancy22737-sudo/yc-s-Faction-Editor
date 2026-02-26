using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactionGearCustomizer.UI.Utils
{
    public class PickerSearchDebouncer
    {
        private float delaySeconds;
        private float lastInputTime;
        private string pendingSearchText;
        private string lastExecutedSearchText;
        private Action<string> onSearch;
        private bool isPending;

        public PickerSearchDebouncer(float delaySeconds, Action<string> onSearch)
        {
            this.delaySeconds = delaySeconds;
            this.onSearch = onSearch;
        }

        public void Update()
        {
            if (!isPending) return;
            
            if (Time.realtimeSinceStartup - lastInputTime >= delaySeconds)
            {
                isPending = false;
                if (pendingSearchText != lastExecutedSearchText)
                {
                    lastExecutedSearchText = pendingSearchText;
                    onSearch?.Invoke(pendingSearchText);
                }
            }
        }

        public void SetSearchText(string text)
        {
            pendingSearchText = text ?? "";
            lastInputTime = Time.realtimeSinceStartup;
            isPending = true;
        }

        public void ForceExecute()
        {
            if (isPending)
            {
                isPending = false;
                lastExecutedSearchText = pendingSearchText;
                onSearch?.Invoke(pendingSearchText);
            }
        }

        public void Clear()
        {
            isPending = false;
            pendingSearchText = "";
            lastExecutedSearchText = "";
        }
    }
}
