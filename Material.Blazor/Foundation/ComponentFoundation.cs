﻿using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Material.Blazor.Internal
{
    public abstract class ComponentFoundation : ComponentBase, IDisposable
    {
        private readonly string[] ReservedAttributes = { "disabled" };
        private readonly string[] EventAttributeNames = { "onfocus", "onblur", "onfocusin", "onfocusout", "onmouseover", "onmouseout", "onmousemove", "onmousedown", "onmouseup", "onclick", "ondblclick", "onwheel", "onmousewheel", "oncontextmenu", "ondrag", "ondragend", "ondragenter", "ondragleave", "ondragover", "ondragstart", "ondrop", "onkeydown", "onkeyup", "onkeypress", "onchange", "oninput", "oninvalid", "onreset", "onselect", "onselectstart", "onselectionchange", "onsubmit", "onbeforecopy", "onbeforecut", "onbeforepaste", "oncopy", "oncut", "onpaste", "ontouchcancel", "ontouchend", "ontouchmove", "ontouchstart", "ontouchenter", "ontouchleave", "ongotpointercapture", "onlostpointercapture", "onpointercancel", "onpointerdown", "onpointerenter", "onpointerleave", "onpointermove", "onpointerout", "onpointerover", "onpointerup", "oncanplay", "oncanplaythrough", "oncuechange", "ondurationchange", "onemptied", "onpause", "onplay", "onplaying", "onratechange", "onseeked", "onseeking", "onstalled", "onstop", "onsuspend", "ontimeupdate", "onvolumechange", "onwaiting", "onloadstart", "ontimeout", "onabort", "onload", "onloadend", "onprogress", "onerror", "onactivate", "onbeforeactivate", "onbeforedeactivate", "ondeactivate", "onended", "onfullscreenchange", "onfullscreenerror", "onloadeddata", "onloadedmetadata", "onpointerlockchange", "onpointerlockerror", "onreadystatechange", "onscroll" };
        private readonly string[] AriaAttributeNames = { "aria-activedescendant", "aria-atomic", "aria-autocomplete", "aria-busy", "aria-checked", "aria-controls", "aria-describedat", "aria-describedby", "aria-disabled", "aria-dropeffect", "aria-expanded", "aria-flowto", "aria-grabbed", "aria-haspopup", "aria-hidden", "aria-invalid", "aria-label", "aria-labelledby", "aria-level", "aria-live", "aria-multiline", "aria-multiselectable", "aria-orientation", "aria-owns", "aria-posinset", "aria-pressed", "aria-readonly", "aria-relevant", "aria-required", "aria-selected", "aria-setsize", "aria-sort", "aria-valuemax", "aria-valuemin", "aria-valuenow", "aria-valuetext" };
        private bool? disabled = null;

        [Inject] private protected IJSRuntime JsRuntime { get; set; }
        [Inject] private protected IMBTooltipService TooltipService { get; set; }
        [Inject] private protected ILogger<ComponentFoundation> Logger { get; set; }


        [CascadingParameter] protected MBCascadingDefaults CascadingDefaults { get; set; } = new MBCascadingDefaults();


        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the created element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object> UnmatchedAttributes { get; set; }




        /// <summary>
        /// Indicates whether the component is disabled.
        /// </summary>
        [Parameter] public bool? Disabled
        {
            get => disabled;
            set
            {
                if (disabled != value)
                {
                    disabled = value;
                    OnDisabledSet?.Invoke(this, null);
                }
            }
        }


        /// <summary>
        /// A markup capable tooltip.
        /// </summary>
        [Parameter] public string Tooltip { get; set; }


        /// <summary>
        /// Tooltip id for aria-describedby attribute.
        /// </summary>
        private Guid TooltipId { get; set; } = Guid.NewGuid();


        /// <summary>
        /// Attributes for splatting to be set by a component's OnInitialized() function.
        /// </summary>
        private protected IDictionary<string, object> ComponentSetAttributes { get; set; } = new Dictionary<string, object>();        
        
        
        /// <summary>
        /// Determines whether to apply the disabled attribute.
        /// </summary>
        internal bool AppliedDisabled => CascadingDefaults.AppliedDisabled(Disabled);


        /// <summary>
        /// Derived components can use this to get a callback from the <see cref="AppliedDisabled"/> setter when the consumer changes the value.
        /// This allows a component to take action with Material Theme js to update the DOM to reflect the data change visually. 
        /// </summary>
        protected event EventHandler OnDisabledSet;


        /// <summary>
        /// Attributes That the component can elect to set for inclusion in SplatAttributes.
        /// </summary>
        private protected IDictionary<string, object> ComponentPureHtmlAttributes { get; set; } = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);


        /// <summary>
        /// Allows a component to build or map out a group of CSS classes to be applied to the component. Use this in <see cref="OnInitialialized()"/>, <see cref="OnParametersSet()"/> or their asynchronous counterparts.
        /// </summary>
        private protected ClassMapper ClassMapper { get; } = new ClassMapper();


        /// <summary>
        /// Allows a component to build or map out a group of HTML styles to be applied to the component. Use this in <see cref="OnInitialialized()"/>, <see cref="OnParametersSet()"/> or their asynchronous counterparts.
        /// </summary>
        private protected StyleMapper StyleMapper { get; } = new StyleMapper();


        /// <summary>
        /// Components should override this with a function to be called when Material.Blazor wants to run Material Theme initialization via JS Interop - always gets called from <see cref="OnAfterRenderAsync()"/>, which should not be overridden.
        /// </summary>
        private protected virtual async Task InitializeMdcComponent() => await Task.CompletedTask;


        ~ComponentFoundation() => Dispose(false);


        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            
            if (disposing && !string.IsNullOrWhiteSpace(Tooltip))
            {
                TooltipService.RemoveTooltip(TooltipId);
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _disposed = true;
        }


        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }



        private readonly string[] stylisticAttributes = { "id", "class", "style" };
        /// <summary>
        /// Attributes ready for splatting in components. Guaranteed not null, unlike UnmatchedAttributes. Default parameter is <see cref="SplatType.All".
        /// </summary>
        internal IReadOnlyDictionary<string, object> AttributesToSplat(SplatType splatType = SplatType.All)
        {
            var allAttributes = new Dictionary<string, object>(ComponentPureHtmlAttributes);
            var idClassAndStyle = new Dictionary<string, object>();
            var htmlAttributes = new Dictionary<string, object>(ComponentPureHtmlAttributes);
            var eventAttributes = new Dictionary<string, object>();
            var requiredAttributes = new Dictionary<string, object>();

            var unmatchedId = (UnmatchedAttributes?.Where(a => a.Key.ToLower() == "id").FirstOrDefault().Value ?? "").ToString();
            var unmatchedClass = (UnmatchedAttributes?.Where(a => a.Key.ToLower() == "class").FirstOrDefault().Value ?? "").ToString();
            var unmatchedStyle = (UnmatchedAttributes?.Where(a => a.Key.ToLower() == "style").FirstOrDefault().Value ?? "").ToString();
            var nonStylisticAttributes = new Dictionary<string, object>(UnmatchedAttributes?.Where(a => !stylisticAttributes.Contains(a.Key.ToLower())) ?? new Dictionary<string, object>());

            // merge ComponentSetAttributes into the dictionary
            nonStylisticAttributes = nonStylisticAttributes.Union(ComponentSetAttributes)
                    .GroupBy(g => g.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.First().Value);

            if (!string.IsNullOrWhiteSpace(Tooltip))
            {
                nonStylisticAttributes.Add("aria-describedby", TooltipId.ToString());
            }

            if (splatType != SplatType.IdClassAndStyleOnly)
            {
                allAttributes = allAttributes.Union(nonStylisticAttributes)
                    .GroupBy(g => g.Key)
                    .ToDictionary(pair => pair.Key, pair => pair.First().Value);

                if (AppliedDisabled) allAttributes.Add("disabled", AppliedDisabled);

                if (splatType == SplatType.ExcludeIdClassAndStyle) return allAttributes;

                htmlAttributes = allAttributes
                                    .Where(kvp => !EventAttributeNames.Contains(kvp.Key))
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (splatType == SplatType.HtmlExcludingIdClassAndStyle) return htmlAttributes;

                eventAttributes = allAttributes
                                    .Where(kvp => EventAttributeNames.Contains(kvp.Key))
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (splatType == SplatType.EventsOnly) return eventAttributes;
            }

            var classString = (ClassMapper.ToString() + " " + unmatchedClass).Trim();
            var styleString = (StyleMapper.ToString() + " " + unmatchedStyle).Trim();

            if (!string.IsNullOrWhiteSpace(unmatchedId)) idClassAndStyle.Add("id", unmatchedId);
            if (!string.IsNullOrWhiteSpace(classString)) idClassAndStyle.Add("class", classString);
            if (!string.IsNullOrWhiteSpace(styleString)) idClassAndStyle.Add("style", styleString);

            foreach (var item in idClassAndStyle)
            {
                if (allAttributes.ContainsKey(item.Key))
                {
                    allAttributes[item.Key] += " " + item.Value;
                }
                else
                {
                    allAttributes.Add(item.Key, item.Value);
                }
            }

            if (splatType == SplatType.IdClassAndStyleOnly)
            {
                return idClassAndStyle;
            }
            else if (splatType == SplatType.All)
            {
                return allAttributes;
            }

            if ((ushort)(splatType & SplatType.IdClassAndStyleOnly) > 0)
            {
                foreach (var item in idClassAndStyle)
                {
                    if (requiredAttributes.ContainsKey(item.Key))
                    {
                        requiredAttributes[item.Key] += " " + item.Value;
                    }
                    else
                    {
                        requiredAttributes.Add(item.Key, item.Value);
                    }
                }
            }

            if ((ushort)(splatType & SplatType.HtmlExcludingIdClassAndStyle) > 0)
            {
                foreach (var item in htmlAttributes)
                {
                    if (requiredAttributes.ContainsKey(item.Key))
                    {
                        requiredAttributes[item.Key] += " " + item.Value;
                    }
                    else
                    {
                        requiredAttributes.Add(item.Key, item.Value);
                    }
                }
            }

            if ((ushort)(splatType & SplatType.EventsOnly) > 0)
            {
                foreach (var item in eventAttributes)
                {
                    requiredAttributes.Add(item.Key, item.Value);
                }
            }

            return requiredAttributes;
        }


        /// <summary>
        /// Material.Blazor components *must always* override this at the start of `OnParametersSet().
        /// </summary>
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            CheckAttributeValidity();
        }


        /// <summary>
        /// Material.Blazor components *must always* override this at the start of `OnParametersSet().
        /// </summary>
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            CheckAttributeValidity();
        }


        /// <summary>
        /// Material.Blazor allows a user to limit unmatched attributes that will be splatted to a defined list in <see cref="MBCascadingDefaults"/>.
        /// This method checks validity against that list.
        /// </summary>
        private void CheckAttributeValidity()
        {
            if (UnmatchedAttributes is null)
            {
                return;
            }

            var reserved = UnmatchedAttributes.Keys.Intersect(ReservedAttributes);

            if (reserved.Count() > 0)
            {
                throw new ArgumentException(
                    $"Material.Blazor: You cannot use {string.Join(", ", reserved.Select(x => $"'{x}'"))} attributes in {Utilities.GetTypeName(GetType())}. Material.Blazor reserves the 'display' HTML attributes for internal use; use the 'Display' parameter instead");
            }

            if (!CascadingDefaults.ConstrainSplattableAttributes)
            {
                return;
            }

            var forbidden =
                    UnmatchedAttributes
                        .Select(kvp => kvp.Key)
                        .Except(EventAttributeNames)
                        .Except(AriaAttributeNames)
                        .Except(CascadingDefaults.AppliedAllowedSplattableAttributes);

            if (forbidden.Count() > 0)
            {
                throw new ArgumentException(
                    $"Material.Blazor: You cannot use {string.Join(", ", forbidden.Select(x => $"'{x}'"))} attributes in {Utilities.GetTypeName(GetType())}. Either remove the attribute or change 'ConstrainSplattableAttributes' or 'AllowedSplattableAttributes' in your MBCascadingDefaults");
            }
        }


        /// <summary>
        /// Material.Blazor components generally *should not* override this because it handles the case where components need
        /// to be adjusted when inside an <c>MBDialog</c> or <c>MBCard</c>. 
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await InitializeMdcComponent();
                
                if (!string.IsNullOrWhiteSpace(Tooltip))
                {
                    TooltipService.AddTooltip(TooltipId, new MarkupString(Tooltip));
                }
            }
        }
    }
}
