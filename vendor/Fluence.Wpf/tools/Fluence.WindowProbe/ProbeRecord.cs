/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Runtime.Serialization;

namespace Fluence.WindowProbe
{
    [DataContract]
    internal sealed class ProbeRecord
    {
        [DataMember] internal string UtcTimestamp { get; set; } = "";
        [DataMember] internal string Phase { get; set; } = "";
        [DataMember] internal string Mode { get; set; } = "";
        [DataMember] internal string RequestedTheme { get; set; } = "";
        [DataMember] internal string RequestedBackdrop { get; set; } = "";
        [DataMember] internal string RequestedRenderMode { get; set; } = "";
        [DataMember] internal int RenderCapabilityTier { get; set; }
        [DataMember] internal int RenderCapabilityTierLevel { get; set; }
        [DataMember] internal bool RequestedTopmost { get; set; }
        [DataMember] internal string TitleArea { get; set; } = "";
        [DataMember] internal double? ContentOriginPhysicalX { get; set; }
        [DataMember] internal double? ContentOriginPhysicalY { get; set; }
        [DataMember] internal string ContentOriginStatus { get; set; } = "";
        [DataMember] internal string WindowBackground { get; set; } = "";
        [DataMember] internal string TargetBackground { get; set; } = "";
        [DataMember] internal string HwndStatus { get; set; } = "";
        [DataMember] internal long? HwndStyle { get; set; }
        [DataMember] internal string HwndStyleStatus { get; set; } = "";
        [DataMember] internal int? HwndStyleWin32Error { get; set; }
        [DataMember] internal int? RectLeft { get; set; }
        [DataMember] internal int? RectTop { get; set; }
        [DataMember] internal int? RectRight { get; set; }
        [DataMember] internal int? RectBottom { get; set; }
        [DataMember] internal int? Dpi { get; set; }
        [DataMember] internal string DpiStatus { get; set; } = "";
        [DataMember] internal bool HighContrast { get; set; }
        [DataMember] internal bool RemoteSession { get; set; }
        [DataMember] internal string OsVersion { get; set; } = "";
        [DataMember] internal string Runtime { get; set; } = "";
        [DataMember] internal int? DwmBackdrop { get; set; }
        [DataMember] internal int? DwmBackdropQueryHResult { get; set; }
        [DataMember] internal string DwmBackdropQueryStatus { get; set; } = "";
        [DataMember] internal int? DwmCorner { get; set; }
        [DataMember] internal int? DwmCornerQueryHResult { get; set; }
        [DataMember] internal string DwmCornerQueryStatus { get; set; } = "";
        [DataMember] internal int? DwmCloaked { get; set; }
        [DataMember] internal int? DwmCloakedQueryHResult { get; set; }
        [DataMember] internal string DwmCloakedQueryStatus { get; set; } = "";
        [DataMember] internal int? ProbeBackdropSetHResult { get; set; }
        [DataMember] internal string ProbeBackdropSetStatus { get; set; } = "";
        [DataMember] internal int? ProbeDarkModeSetHResult { get; set; }
        [DataMember] internal int? ProbeDarkModePrimaryHResult { get; set; }
        [DataMember] internal int? ProbeDarkModeFallbackHResult { get; set; }
        [DataMember] internal string ProbeDarkModeSetStatus { get; set; } = "";
        [DataMember] internal int? ProbeCornerSetHResult { get; set; }
        [DataMember] internal string ProbeCornerSetStatus { get; set; } = "";
        [DataMember] internal string FluenceOwnedCallStatus { get; set; } = "unobserved";
    }
}
