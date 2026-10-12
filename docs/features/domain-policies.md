---
title: Domain Policies
parent: Features
nav_order: 3
---

# Domain policies

Domain policies assign a dedicated Content Security Policy (CSP) to a content node that has hostnames configured under **Culture and Hostnames**, typically a site root. When enabled, a domain policy overrides the default **Frontend** policy for requests routed through any of that node's hostnames; all other hostnames continue using the Frontend policy.

A policy belongs to the node, not to a single hostname. A multilingual site with one hostname per culture (`example.com/nl`, `example.com/fr`, `example.com/de`) has one policy that covers all of them, including cultures added later.

Use domain policies when specific sites require unique directives—such as dedicated analytics on a microsite or third-party embeds on a campaign page.

## Create a domain policy

1. Open the **CSP Management** section in the backoffice.
2. In the tree, open the context menu (`...`) on the **Frontend** node and select **Add Domain Policy**.
3. Select a content node. The list shows every node that has at least one hostname and no domain policy yet, with its hostnames.
4. Adjust directives in the editor (pre-filled with a copy of the Frontend policy), then click **Save**. A new domain policy is always enabled, even when the Frontend policy is disabled.

The policy is created once saved and appears as a child of the Frontend node in the tree.

Domain policies support the same settings (Report-Only, Reporting, Upgrade Insecure Requests, Sources, and Evaluator) as global policies. The editor lists the hostnames the policy applies to; click **Open content item** to navigate directly to the content node.

## Disable or re-enable a policy

Toggle the status switch under **Settings** to disable a domain policy without deleting it:

- **Disabled:** Displays as `(inactive)` in the tree. Requests fall back to the Frontend policy or emit no header, depending on [`DisabledNodePolicyBehavior`](configuration#disablednodepolicybehavior).
- **Enabled:** The domain policy applies to requests on every hostname of the node.

{: .warning }
A CSP does not provide an origin isolation boundary. Path-based cultures on the same origin (such as `example.com/en` and `example.com/fr`) share execution context in the browser. To isolate policies, use separate hostnames.

{: .note }
Only nodes with at least one hostname can receive a domain policy. Culture-only wildcard entries don't count, as they only set the culture and never route a request. Each node supports one policy.

{: .note }
Nested sites work as you would expect. If `example.com/docs` is assigned to a node below the `example.com` root, it is routed as its own site: it gets the policy of the docs node, or the Frontend policy if that node has none. It does not inherit the root's policy.

## Policy resolution order

The policy is resolved from the hostname Umbraco routed the request through (`PublishedRequest.Domain`) and the content node it is assigned to:

| Request context | Applied policy |
|---|---|
| Backoffice (`/umbraco`) | Backoffice policy (domain policies never apply) |
| Hostname of a node with an active domain policy | Domain policy |
| Hostname of a node with a disabled domain policy | Frontend policy, or no header ([`DisabledNodePolicyBehavior`](configuration#disablednodepolicybehavior)) |
| Hostname of a node without a domain policy | Frontend policy |
| Unrouted requests (static assets, custom APIs, unmatched hosts) | Frontend policy |

## Delete a policy

Open the policy, click **Delete domain policy**, and confirm. Traffic on the node's hostnames reverts to the Frontend policy immediately. Global Frontend and Backoffice policies cannot be deleted.

## Renamed hostnames, removed hostnames and deleted content

Because a policy belongs to the content node, renaming a hostname (for example `example.com` to `www.example.com`) or adding and removing cultures has no effect on it: the policy keeps applying to whatever hostnames the node has.

A policy only stops applying when its node has no hostname left, or the node is moved to the recycle bin or deleted:

- The policy is kept rather than deleted, and appears in the tree as **Node name (no hostname)**, or **Deleted content (...)** when the node itself is gone. A node in the recycle bin keeps its hostnames in Culture and Hostnames, but Umbraco doesn't route requests to it, so its policy shows as **Node name (no hostname)** too.
- Assigning a hostname to the node again, or restoring it from the recycle bin, brings the policy back into use automatically.
- If the node is permanently decommissioned, click **Delete domain policy** to remove the orphan.

## Load balancing and uSync

- **Cache invalidation:** Saving or deleting a domain policy clears the CSP cache across all load-balanced instances automatically.
- **[uSync](../integrations/usync):** Domain policies export to the `CspNodePolicies` folder in the uSync **Content** group, importing after their content node and hostnames. The exported file references the content node by key, which uSync keeps identical across environments.

## Extending

- [`CspWritingNotification`](../advanced/notification-events#cspwritingnotification) supplies the resolved policy instance. Inspect `CspDefinition.ContentKey` (the key of the content node) to distinguish domain policies from the global Frontend policy.
- [`CspDeletedNotification`](../advanced/notification-events#cspdeletednotification) fires whenever a domain policy is deleted.