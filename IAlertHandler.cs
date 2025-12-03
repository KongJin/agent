using System;
using OpenQA.Selenium;

namespace WebAgentCli;

/// <summary>
/// Handles browser-level alerts/permission prompts (accept/dismiss).
/// </summary>
public interface IAlertHandler
{
    /// <summary>Accepts the alert if present. Returns true when an alert was accepted.</summary>
    bool TryAcceptIfPresent();
    /// <summary>Dismisses the alert if present. Returns true when an alert was dismissed.</summary>
    bool TryDismissIfPresent();
}
