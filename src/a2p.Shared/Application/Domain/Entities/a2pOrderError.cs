﻿using a2p.Shared.Application.Services.Domain.Enums;
using a2p.Shared.Domain.Enums;

namespace a2p.Shared.Application.Services.Domain.Entities
{
    public class A2POrderError
    {
        required public string Order { get; set; }


        required public ErrorLevel Level { get; set; }
        required public ErrorCode Code { get; set; }


        required public string Message { get; set; }


        // ======================================
        // 🟢 Common Errors
        // ======================================
        // ======================================
        // 🔵 Read Process Errors
        // ======================================

    }
}
