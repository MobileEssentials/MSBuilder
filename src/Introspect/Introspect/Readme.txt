MSBuilder: Introspect
=========================================

Allows retrieving the current project's properties and currently building 
targets as items and metadata, effectivey allowing retrieval of property 
values dynamically by name.

Usage:

<Introspect>
  <Output TaskParameter="Properties" ItemName="CurrentProperties" />  
  <Output TaskParameter="Targets" ItemName="CurrentTargets" />  
</Introspect>

<PropertyGroup>
  <!-- Note that we're using another property as the dynamic property name to evaluate -->
  <PropertyValue>@(CurrentProperties -> Metadata("$(PropertyName)"))</PropertyValue>
</PropertyGroup>
