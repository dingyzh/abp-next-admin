﻿using System;
using System.Collections.Generic;
using Volo.Abp.Collections;

namespace LINGYUN.Abp.Webhooks;

public class AbpWebhooksOptions
{
    /// <summary>
    /// 默认超时时间
    /// </summary>
    public TimeSpan TimeoutDuration { get; set; }
    /// <summary>
    /// 默认最大发送次数
    /// </summary>
    public int MaxSendAttemptCount { get; set; }
    /// <summary>
    /// 是否达到最大连续失败次数时自动取消订阅
    /// </summary>
    public bool IsAutomaticSubscriptionDeactivationEnabled { get; set; }
    /// <summary>
    /// 取消订阅前最大连续失败次数
    /// </summary>
    public int MaxConsecutiveFailCountBeforeDeactivateSubscription { get; set; }

    public ITypeList<IWebhookDefinitionProvider> DefinitionProviders { get; }

    public HashSet<string> DeletedWebhooks { get; }

    public HashSet<string> DeletedWebhookGroups { get; }
    /// <summary>
    /// 默认请求头
    /// </summary>
    public IDictionary<string, string> DefaultHttpHeaders { get; }

    public AbpWebhooksOptions()
    {
        TimeoutDuration = TimeSpan.FromSeconds(60);
        MaxSendAttemptCount = 5;
        MaxConsecutiveFailCountBeforeDeactivateSubscription = MaxSendAttemptCount * 3;

        DefinitionProviders = new TypeList<IWebhookDefinitionProvider>();

        DeletedWebhooks = new HashSet<string>();
        DeletedWebhookGroups = new HashSet<string>();

        DefaultHttpHeaders = new Dictionary<string, string>
        {
            // 取消响应内容包装
            { "_AbpDontWrapResult", "true" },
            // TODO: 可能跨域影响
            // { "X-Requested-With", "XMLHttpRequest" },
            // 标识来源
            { "X-Requested-From", "abp-webhooks" },
        };
    }

    public void AddHeader(string key, string value)
    {
        if (value.IsNullOrWhiteSpace())
        {
            return;
        }
        DefaultHttpHeaders[key] = value;
    }
}
