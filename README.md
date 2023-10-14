# Umbraco CSP (Content security policy) manager

[![Platform](https://img.shields.io/badge/Umbraco-10.3+-%233544B1?style=flat&logo=umbraco)](https://umbraco.com/products/umbraco-cms/)
[![GitHub](https://img.shields.io/github/license/Matthew-Wise/Umbraco-CSP-manager)](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/LICENSE)
[![.NET](https://github.com/Matthew-Wise/Umbraco-CSP-manager/actions/workflows/main.yml/badge.svg?event=push)](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/.github/workflows/main.yml)

Enables you to manage CSP for both the front and back end, via CMS section.

![Management CSP section](https://github.com/Matthew-Wise/Umbraco-CSP-manager/blob/main/images/managment-screen.png "Csp Management screen")

## Installation

```
dotnet add package Umbraco.Community.CSPManager
```

You will also need to give access via the users section to the CSP Manager section.

## Nonce Tag Helper

To use CSP nonce you can make use of the Tag Helper.

First you will need to include the namespace in the `ViewImports.cshtml`

```
@addTagHelper *, Umbraco.Community.CSPManager
```

To use the nonce add the following to your `<script>` or `<style>` tags:

```
csp-manager-add-nonce="true"
```

When this is added it will include the nonce in the CSP header and output in the page.

If you need to access the nonce within a data attribute you can  use the following:

```
csp-manager-add-nonce-data-attribute="true"
```