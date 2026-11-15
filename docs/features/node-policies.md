---
title: Node Policies
parent: Features
nav_order: 3
---

# Node policies

A node policy gives a content node its own Content Security Policy (CSP): a whole site, a section or a single page. It applies to the node and, unless you say otherwise, to the pages below it. Everything else keeps the **Frontend** policy.

Use node policies when part of your content needs different sources, such as a microsite with its own analytics, a campaign page with third-party embeds, or one language of a site that loads a regional consent tool.

A node policy has three choices:

| Choice | Options |
|---|---|
| Culture | **All cultures** (the default), or one language of the site |
| Policy type | **Complete policy**, which replaces what the content inherits, or **Inherit and add**, which only adds sources to it |
| Applies to | **This content and the pages below it** (the default), or **This content only** |

## Create a node policy

1. Open the **CSP Manager** section in the backoffice.
2. In the tree, hover over the **Frontend** node and click **Add Node Policy** (also in its `...` menu). You can also click **Add Node Policy** on the CSP Manager dashboard.
3. Choose the content. Pick any node from the content tree, or click one of the sites listed below the picker (the nodes that have a hostname in **Culture and Hostnames**).
4. Choose the culture and the policy type, then click **Continue**.
5. Edit the sources, then click **Save**.

Nothing is stored until you save. The saved policy appears below the Frontend node in the tree, named after its content and, for a single culture, that culture: `Products (en-GB)`. Inherit-and-add policies have a plus icon.

A new policy is always switched on. A node can have one policy for all cultures and one for each language.

## Complete policy or inherit and add

| | Complete policy | Inherit and add |
|---|---|---|
| Contains | Every source and setting, like the Frontend policy | Only the extra sources |
| Starts as | A copy of the policy the content inherits today | Empty |
| Changes further up the tree | Don't reach this content | Reach this content |
| Report-only, reporting and upgrade insecure requests | Set on the policy | Come from the policy it adds to |

Choose **Inherit and add** when a page needs one or two more sources and should otherwise follow the rest of the site. Choose **Complete policy** when the content needs a policy of its own, or needs fewer sources than it inherits.

An inherit-and-add policy can only allow more, never less:

- Its sources are added to the directives of the inherited policy.
- When it uses a fetch directive the inherited policy doesn't have, that directive starts from the sources of the directive the browser falls back to. For example, adding `cdn.example.com` to `script-src` on top of `default-src 'self'` gives `script-src 'self' cdn.example.com`. Without this, the new directive would switch off the fallback and block what was allowed before.
- `'none'` is dropped from a directive once another source is added to it.

You can change what the policy applies to later under **Settings**. The content, the culture and the policy type are fixed: delete the policy and create a new one to change them.

{: .note }
A complete policy for one culture starts as a copy of what the content gets in that culture, so saving it unchanged changes no header. A complete policy for all cultures starts from the all-cultures policy the content inherits. If a node further up has a policy for one culture, requests in that culture get the new policy from then on, so check **Effective** for each culture after saving.

## Which policy a request gets

For a page request, CSP Manager starts at the requested page and walks up the content tree:

1. On each node it looks for a policy for the culture of the request, then for the languages that culture falls back to, then for the all-cultures policy.
2. A policy of a node further up only counts when it applies to the pages below it.
3. Inherit-and-add policies are collected along the way. The walk stops at the first complete policy.
4. If there is no complete policy, the Frontend policy is the starting point.

The response gets the complete policy (or the Frontend policy) with the sources of the collected inherit-and-add policies added, nearest the page last.

| Request | Applied policy |
|---|---|
| Backoffice (`/umbraco`) | Backoffice policy (node policies never apply) |
| Page with its own complete policy | That policy |
| Page below a node with a complete policy | That policy, unless it is for that node only |
| Page with an inherit-and-add policy | The policy it inherits, plus the extra sources |
| Page with no policy on it or above it | Frontend policy |
| Unrouted requests (static assets, custom APIs, unmatched hosts) | Frontend policy |

{: .note }
A page can be served on the hostname of a site it isn't stored under, such as a shared "not found" page. The visitor is on that site, so the page gets the policies of the hostname's node and the nodes above it, not those of the nodes it is stored under.

{: .warning }
A CSP does not provide an origin isolation boundary. Pages and path-based cultures on the same origin (such as `example.com/en` and `example.com/fr`) share an execution context in the browser. To isolate content, use separate hostnames.

## See what a page gets

Open a node policy and go to **Effective**. It shows the header a request for that content gets today, worked out from the saved policies, and which policy it comes from. Click **Open that policy** to go to it.

- On a multilingual site, choose the culture of the request. No culture means the default language, as for a request.
- Changes you haven't saved are not included. For a new policy that isn't saved yet, the view shows what the content gets without it.
- Nonces are different for each response, so they are not shown. Neither are changes your own code makes to a response in a [`CspWritingNotification`](../advanced/notification-events#cspwritingnotification) handler.

For an inherit-and-add policy, **Evaluate** checks this effective policy too, because the extra sources are not a policy on their own.

## Disable or re-enable a policy

Toggle the status switch under **Settings** to switch a node policy off without deleting it. It displays as `(inactive)` in the tree.

- **Complete policy:** the content gets the policy it inherits (the nearest policy further up the tree, or the Frontend policy), or no header, depending on [`DisabledNodePolicyBehavior`](configuration#disablednodepolicybehavior).
- **Inherit and add:** its extra sources are no longer added. The content keeps the policy it inherits.

## Delete a policy

Open the policy, click **Delete node policy**, and confirm. Its content gets the policy it inherits immediately. The global Frontend and Backoffice policies cannot be deleted.

## Hostnames, deleted content and removed languages

A policy belongs to the content node, not to a hostname. Renaming a hostname (for example `example.com` to `www.example.com`) has no effect on it, and a site with a hostname per culture needs only one policy.

A policy stops applying when its node is deleted or moved to the recycle bin, or when its culture is no longer a language of the site:

- The policy is kept, and appears in the tree as **Node name (not in use)**, or **Deleted content (...)** when the node itself is gone.
- Restoring the node from the recycle bin, or adding the language again, brings the policy back into use automatically.
- If the content is gone for good, click **Delete node policy** to remove the policy.

## Upgrading from 18.1

Upgrading adds node policies without changing your existing ones. The Frontend and Backoffice policies keep working as before, and pages without a node policy keep the Frontend policy.

## Load balancing and uSync

- **Cache invalidation:** Saving or deleting a node policy clears the CSP cache across all load-balanced instances automatically.
- **[uSync](../integrations/usync):** Node policies export to the `CspNodePolicies` folder in the uSync **Content** group, importing after their content node. The exported file references the content node by key, which uSync keeps identical across environments.

## Extending

- [`CspWritingNotification`](../advanced/notification-events#cspwritingnotification) supplies the resolved policy. Inspect `CspDefinition.ContentKey` (the key of the content node) to tell a node policy from the global Frontend policy. For a merged result, the key and culture are those of the policy nearest the page.
- [`CspDeletedNotification`](../advanced/notification-events#cspdeletednotification) fires whenever a node policy is deleted.
