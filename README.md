# ProtoCTask

```xml
<PropertyGroup>
  <ProtoCTaskDir>path\to\directory\containing\ProtoC.props</ProtoCTaskDir>
</PropertyGroup>
<Import Project="$(ProtoCTaskDir)\ProtoC.props" />
<Import Project="$(ProtoCTaskDir)\ProtoC.targets" />

...

<ItemGroup>
  <Protobuf Include="File.proto" />
</ItemGroup>
```

**NOTE**: _Currently requires bin/protoc.exe to be added to the staging directory manually._

* Automatically generates `.pb.cc` files and feeds them to `ClCompile`.
* Adds the directory containing the `.pb.h` files to the `ClCompile` include path.
* Does dependency tracking for minimal rebuilds.
