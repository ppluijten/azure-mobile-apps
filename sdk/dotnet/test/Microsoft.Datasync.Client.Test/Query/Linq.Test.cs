﻿// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.Datasync.Client.Query;
using Microsoft.Datasync.Client.Table;
using Microsoft.Datasync.Client.Test.Helpers;
using System.Linq.Expressions;

namespace Microsoft.Datasync.Client.Test.Query;

[ExcludeFromCodeCoverage]
public class Linq_Tests : ClientBaseTest
{
    DatasyncClient _client;
    RemoteTable<KitchenSink> _table;
    TableQuery<KitchenSink> _query;

    public Linq_Tests()
    { 
        _client = GetMockClient();
        _table = _client.GetRemoteTable<KitchenSink>("kitchensink") as RemoteTable<KitchenSink>;
        _query = new TableQuery<KitchenSink>(_table);
    }

    [Fact]
    public void Linq_DerivedTest()
    {
        var client = GetMockClient();
        var table = client.GetRemoteTable<DerivedTestDatasyncEntity>("derived");
        var queryString = GetDerivedQuery(table);
        Assert.Equal("$filter=deleted", queryString);
    }

    public static string GetDerivedQuery<T>(IRemoteTable<T> table) where T : IDatasyncEntity
    {
        return table.Where(x => x.Deleted).ToODataString();
    }

    [Fact]
    public void Linq_EndsWith_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.EndsWith("abc"),
            "$filter=endswith(stringProperty,'abc')"
        );
    }

    [Fact]
    public void Linq_EndsWith_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.EndsWith("abc", StringComparison.Ordinal),
            "$filter=endswith(stringProperty,'abc')"
        );
    }

    [Fact]
    public void Linq_EndsWith_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.EndsWith("abc", StringComparison.InvariantCulture),
            "$filter=endswith(stringProperty,'abc')"
        );
    }

    [Fact]
    public void Linq_EndsWith_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.EndsWith("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=endswith(tolower(stringProperty),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_EndsWith_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.EndsWith("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=endswith(tolower(stringProperty),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Equals_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.Equals("abc"),
            "$filter=(stringProperty eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Equals_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.Equals("abc", StringComparison.Ordinal),
            "$filter=(stringProperty eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Equals_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.Equals("abc", StringComparison.InvariantCulture),
            "$filter=(stringProperty eq 'abc')"
        );
    }

    [Fact]
    public void Linq_Equals_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.Equals("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=(tolower(stringProperty) eq tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_Equals_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.Equals("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=(tolower(stringProperty) eq tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_StartsWith_NoStringComparison()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.StartsWith("abc"),
            "$filter=startswith(stringProperty,'abc')"
        );
    }

    [Fact]
    public void Linq_StartsWith_Ordinal()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.StartsWith("abc", StringComparison.Ordinal),
            "$filter=startswith(stringProperty,'abc')"
        );
    }

    [Fact]
    public void Linq_StartsWith_Invariant()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.StartsWith("abc", StringComparison.InvariantCulture),
            "$filter=startswith(stringProperty,'abc')"
        );
    }

    [Fact]
    public void Linq_StartsWith_OrdinalIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.StartsWith("abc", StringComparison.OrdinalIgnoreCase),
            "$filter=startswith(tolower(stringProperty),tolower('abc'))"
        );
    }

    [Fact]
    public void Linq_StartsWith_InvariantIgnoreCase()
    {
        ExecuteWhereQuery(
            m => m.StringProperty.StartsWith("abc", StringComparison.InvariantCultureIgnoreCase),
            "$filter=startswith(tolower(stringProperty),tolower('abc'))"
        );
    }

    private void ExecuteWhereQuery(Expression<Func<KitchenSink, bool>> predicate, string expected)
    {
        var sut = _query.Where(predicate);
        var actual = ((TableQuery<KitchenSink>)sut).ToODataString(true);
        Assert.Equal(expected, actual);
    }

    public class KitchenSink : DatasyncClientData
    {
        public string StringProperty { get; set; }
    }
}
