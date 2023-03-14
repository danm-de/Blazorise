﻿#region Using directives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicTestApp.Client;
using Blazorise.DataGrid;
using Blazorise.Tests.Extensions;
using Blazorise.Tests.Helpers;
using Blazorise.Tests.TestServices;
using Bunit;
using FluentAssertions;
using Xunit;
using Employee = BasicTestApp.Client.DataGridComponent.Employee;
#endregion

namespace Blazorise.Tests.Components;

public class DataGridComponentTest : TestContext
{
    public DataGridComponentTest()
    {
        BlazoriseConfig.AddBootstrapProviders( Services );
        BlazoriseConfig.JSInterop.AddDataGrid( this.JSInterop );
    }

    [Fact]
    public void NoData_Should_Render_EmptyTemplate()
    {
        // setup
        var expectedEmptyTemplate = "No Records...";

        // test
        var comp = RenderComponent<DataGridComponent>( parameters =>
            parameters.Add( x => x.Data, null ) );

        // validate
        var emptyTemplateValue = comp.Find( "tbody tr td" ).TextContent;

        Assert.Contains( expectedEmptyTemplate, emptyTemplateValue );
    }

    [Fact]
    public void SortByField_Should_CorrectlySortRows()
    {
        // setup
        var expectedOrderedValues = new[] { "1/8", "1/4", "1/3", "1/2", "3/4" };

        // test
        var comp = RenderComponent<DataGridComponent>();

        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public void SortByField_Should_CorrectlyReorderRows()
    {
        // setup
        var expectedOrderedValues = new[] { "3/4", "1/2", "1/3", "1/4", "1/8"  };
        var comp = RenderComponent<DataGridComponent>();

        // test
        comp.Find( "thead tr th:nth-child(2)" )
            .Click(); // change sort order to descending
        
        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public async Task SortByField_Should_RaiseSortChangedEvent()
    {
        // setup
        var sortChanged = new List<DataGridSortChangedEventArgs>();
        var comp = RenderComponent<DataGridComponent>( parameters =>
        {
            parameters.Add(
                parameterSelector: x => x.SortChanged,
                callback: e => sortChanged.Add( e ) );
        } );

        // test
        await Task.Factory.StartNew(
            // Note: event handling was implemented using Fire & Forget (async call without await keyword!)
            () => comp.Find( "thead tr th:nth-child(2)" ).Click(), // change sort order to descending
            cancellationToken: CancellationToken.None,
            creationOptions: TaskCreationOptions.None,
            scheduler: new CurrentThreadTaskScheduler()
        );

        // validate
        sortChanged
            .Should().HaveCount( 1 )
            .And
            .OnlyContain( e => e.ColumnFieldName == "Fraction" &&
                               e.FieldName == "FractionValue" &&
                               e.SortDirection == SortDirection.Descending );
    }

    [Fact]
    public async Task SortByField_Should_RaiseSortingChangedEvent()
    {
        // setup
        var sortingChanged = new List<DataGridSortingChangedEventArgs>();
        var comp = RenderComponent<DataGridComponent>( parameters =>
        {
            parameters.Add(
                parameterSelector: x => x.SortingChanged,
                callback: e => sortingChanged.Add( e ) );
        } );

        // test
        await Task.Factory.StartNew(
            // Note: event handling was implemented using Fire & Forget (async call without await keyword!)
            action: () => comp.Find( "thead tr th:nth-child(2)" ).Click(), // change sort order to descending
            cancellationToken: CancellationToken.None,
            creationOptions: TaskCreationOptions.None,
            scheduler: new CurrentThreadTaskScheduler()
        );

        // validate
        sortingChanged
            .Should().HaveCount( 1 )
            .And
            .ContainEquivalentOf( new DataGridSortingChangedEventArgs(
                sortMode: DataGridSortMode.Multiple,
                columns: new[]
                {
                    new DataGridSortColumnInfo(
                        field: nameof(Employee.Fraction),
                        sortField: nameof(Employee.FractionValue),
                        sortDirection: SortDirection.Descending,
                        sortOrder: 0 )
                } )
            );
    }

    [Fact]
    public async Task Sort_Should_CorrectlyReorderWhenUsingFieldName()
    {
        // setup
        var expectedOrderedValues = new[] { "3/4", "1/2", "1/3", "1/4", "1/8" };
        var comp = RenderComponent<DataGridComponent>();
        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await Task.Factory.StartNew(
            // Note: event handling was implemented using Fire & Forget (async call without await keyword!)
            action: () => dataGrid.Instance.Sort( nameof(Employee.Fraction), SortDirection.Descending )
                .GetAwaiter().GetResult(),
            cancellationToken: CancellationToken.None,
            creationOptions: TaskCreationOptions.None,
            scheduler: new CurrentThreadTaskScheduler()
        );

        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public async Task ApplySorting_Multiple_Should_ReorderCorrectlyWhenSortingWithMultipleColumns()
    {
        // setup
        var expectedOrderedValues = new[] { "3/4", "1/3", "1/2", "1/8", "1/4" };
        var comp = RenderComponent<DataGridComponent>();
        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await dataGrid.Instance.ApplySorting(
            new DataGridSortColumn(
                field: nameof(Employee.Name),
                sortDirection: SortDirection.Ascending ),
            new DataGridSortColumn(
                field: nameof(Employee.Fraction),
                sortDirection: SortDirection.Ascending )
        );

        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public async Task ApplySorting_Multiple_Should_RaiseSortingChangedEvent()
    {
        // setup
        var sortingChanged = new List<DataGridSortingChangedEventArgs>();
        var comp = RenderComponent<DataGridComponent>( parameters =>
        {
            parameters.Add(
                parameterSelector: x => x.SortingChanged,
                callback: e => sortingChanged.Add( e ) );
        } );

        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await dataGrid.Instance.ApplySorting(
            new DataGridSortColumn(
                field: nameof(Employee.Name),
                sortDirection: SortDirection.Ascending ),
            new DataGridSortColumn(
                field: nameof(Employee.Fraction),
                sortDirection: SortDirection.Descending )
        );

        // validate
        sortingChanged
            .Should().HaveCount( 1 )
            .And
            .ContainEquivalentOf( new DataGridSortingChangedEventArgs(
                sortMode: DataGridSortMode.Multiple,
                columns: new[]
                {
                    new DataGridSortColumnInfo(
                        field: nameof(Employee.Name),
                        sortField: nameof(Employee.Name),
                        sortDirection: SortDirection.Ascending,
                        sortOrder: 0 ),
                    new DataGridSortColumnInfo(
                        field: nameof(Employee.Fraction),
                        sortField: nameof(Employee.FractionValue),
                        sortDirection: SortDirection.Descending,
                        sortOrder: 1 )
                } )
            );
    }

    [Fact]
    public async Task ApplySorting_Multiple_Should_RestoreTheDefaultOrderIfNoSortColumnsAreSpecified()
    {
        // setup
        var expectedOrderedValues = new[] { "1/2", "1/4", "1/8", "3/4", "1/3" };
        var comp = RenderComponent<DataGridComponent>();
        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await dataGrid.Instance.ApplySorting(Array.Empty<DataGridSortColumn>());

        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public async Task ApplySorting_Single_Should_ReorderCorrectlyWhenSortingASingleColumn()
    {
        // setup
        var expectedOrderedValues = new[] { "3/4", "1/2", "1/3", "1/8", "1/4" };
        var comp = RenderComponent<DataGridComponent>( p => p.Add( x => x.SortMode, DataGridSortMode.Single ) );
        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await dataGrid.Instance.ApplySorting(
            new DataGridSortColumn(
                field: nameof(Employee.Name),
                sortDirection: SortDirection.Ascending )
        );

        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public async Task ApplySorting_Single_Should_RestoreTheDefaultOrderIfNoSortColumnIsSpecified()
    {
        // setup
        var expectedOrderedValues = new[] { "1/2", "1/4", "1/8", "3/4", "1/3" };
        var comp = RenderComponent<DataGridComponent>( p => p.Add( x => x.SortMode, DataGridSortMode.Single ) );
        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await dataGrid.Instance.ApplySorting( Array.Empty<DataGridSortColumn>() );

        // validate
        comp.FindAll( "tbody tr td:nth-child(2)" )
            .Select( x => x.TextContent )
            .Should()
            .BeEquivalentTo( expectedOrderedValues );
    }

    [Fact]
    public async Task ApplySorting_Single_Should_RaiseSortingChangedEvent()
    {
        // setup
        var sortingChanged = new List<DataGridSortingChangedEventArgs>();
        var comp = RenderComponent<DataGridComponent>( parameters =>
        {
            parameters.Add( x => x.SortMode, DataGridSortMode.Single );
            parameters.Add(
                parameterSelector: x => x.SortingChanged,
                callback: e => sortingChanged.Add( e ) );
        } );

        var dataGrid = comp.FindComponent<DataGrid<Employee>>();

        // test
        await dataGrid.Instance.ApplySorting(
            new DataGridSortColumn(
                field: nameof(Employee.Name),
                sortDirection: SortDirection.Descending )
        );

        // validate
        sortingChanged
            .Should().HaveCount( 1 )
            .And
            .ContainEquivalentOf( new DataGridSortingChangedEventArgs(
                sortMode: DataGridSortMode.Single,
                columns: new[]
                {
                    new DataGridSortColumnInfo(
                        field: nameof(Employee.Name),
                        sortField: nameof(Employee.Name),
                        sortDirection: SortDirection.Descending,
                        sortOrder: 0 )
                } )
            );
    }

    [Fact]
    public async Task RemoveColumn_Should_RaiseSortingChanged()
    {
        // setup
        var sortingChanged = new List<DataGridSortingChangedEventArgs>();
        var comp = RenderComponent<DataGridComponent>( parameters =>
        {
            parameters.Add(
                parameterSelector: x => x.SortingChanged,
                callback: e => sortingChanged.Add( e ) );
        } );
        var dataGrid = comp.FindComponent<DataGrid<Employee>>();
        var column = dataGrid
            .FindComponents<DataGridColumn<Employee>>()
            .Single( x => x.Instance.Field == nameof(Employee.Fraction) );

        // test
        await Task.Factory.StartNew(
            // Note: event handling was implemented using Fire & Forget (async call without await keyword!)
            function: () => dataGrid.Instance.RemoveColumn( column.Instance ),
            cancellationToken: CancellationToken.None,
            creationOptions: TaskCreationOptions.None,
            scheduler: new CurrentThreadTaskScheduler()
        );

        // validate
        sortingChanged
            .Should().HaveCount( 1 )
            .And
            .ContainEquivalentOf( new DataGridSortingChangedEventArgs(
                sortMode: DataGridSortMode.Multiple,
                columns: Array.Empty<DataGridSortColumnInfo>() )
            );
    }

    [Theory]
    [InlineData( DataGridEditMode.Form )]
    [InlineData( DataGridEditMode.Inline )]
    [InlineData( DataGridEditMode.Popup )]
    public void New_Should_AddNewItem( DataGridEditMode editMode )
    {
        // setup
        var comp = RenderComponent<DataGridComponent>( parameters =>
            parameters.Add( x => x.DataGridEditMode, editMode ) );
        var startingDataCount = comp.Instance.Data.Count;

        // test
        comp.Click( "#btnNew" );
        comp.Click( "#btnSave" );
        var currentDataCount = comp.Instance.Data.Count;

        // validate
        var expectedResult = startingDataCount + 1;
        comp.WaitForAssertion( () => Assert.Equal( expectedResult, comp.Instance.Data.Count ), System.TimeSpan.FromSeconds( 3 ) );
    }

    [Theory]
    [InlineData( DataGridEditMode.Form )]
    [InlineData( DataGridEditMode.Inline )]
    [InlineData( DataGridEditMode.Popup )]
    public void Edit_Should_UpdateItem( DataGridEditMode editMode )
    {
        // setup
        var updatedName = "RaulFromEdit";
        var comp = RenderComponent<DataGridComponent>( parameters =>
            parameters.Add( x => x.DataGridEditMode, editMode ) );

        // test
        comp.Find( "#btnEdit" ).Click();

        comp.Input( "input", updatedName,
            ( firstInput ) => firstInput.SetAttribute( "value", updatedName ) );

        comp.Click( "#btnSave" );

        var currentName = comp.Instance.Data[0].Name;

        // validate
        comp.WaitForAssertion( () => Assert.Contains( comp.Instance.Data, x => x.Name == updatedName ), System.TimeSpan.FromSeconds( 3 ) );
    }

    [Theory]
    [InlineData( DataGridEditMode.Form )]
    [InlineData( DataGridEditMode.Inline )]
    [InlineData( DataGridEditMode.Popup )]
    public void Delete_Should_DeleteItem( DataGridEditMode editMode )
    {
        // setup
        var comp = RenderComponent<DataGridComponent>( parameters =>
            parameters.Add( x => x.DataGridEditMode, editMode ) );
        var startingDataCount = comp.Instance.Data.Count;

        // test
        comp.Click( "#btnDelete" );

        var currentDataCount = comp.Instance.Data.Count;

        // validate
        var expectedResult = startingDataCount - 1;
        comp.WaitForAssertion( () => Assert.Equal( expectedResult, currentDataCount ), System.TimeSpan.FromSeconds( 3 ) );
    }

}