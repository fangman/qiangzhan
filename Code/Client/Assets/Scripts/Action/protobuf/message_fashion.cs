//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: message_fashion.hxx
namespace Message
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"request_active_fashion")]
  public partial class request_active_fashion : global::ProtoBuf.IExtensible
  {
    public request_active_fashion() {}
    
    private int _fashionid;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"fashionid", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int fashionid
    {
      get { return _fashionid; }
      set { _fashionid = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"respond_active_fashion")]
  public partial class respond_active_fashion : global::ProtoBuf.IExtensible
  {
    public respond_active_fashion() {}
    
    private int _errorcode;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"errorcode", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int errorcode
    {
      get { return _errorcode; }
      set { _errorcode = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"request_addstar_fashion")]
  public partial class request_addstar_fashion : global::ProtoBuf.IExtensible
  {
    public request_addstar_fashion() {}
    
    private int _fashionid;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"fashionid", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int fashionid
    {
      get { return _fashionid; }
      set { _fashionid = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"respond_addstar_fashion")]
  public partial class respond_addstar_fashion : global::ProtoBuf.IExtensible
  {
    public respond_addstar_fashion() {}
    
    private int _errorcode;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"errorcode", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int errorcode
    {
      get { return _errorcode; }
      set { _errorcode = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"request_equip_fashion")]
  public partial class request_equip_fashion : global::ProtoBuf.IExtensible
  {
    public request_equip_fashion() {}
    
    private int _fashionid;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"fashionid", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int fashionid
    {
      get { return _fashionid; }
      set { _fashionid = value; }
    }
    private int _action;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"action", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int action
    {
      get { return _action; }
      set { _action = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"respond_equip_fashion")]
  public partial class respond_equip_fashion : global::ProtoBuf.IExtensible
  {
    public respond_equip_fashion() {}
    
    private int _errorcode;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"errorcode", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int errorcode
    {
      get { return _errorcode; }
      set { _errorcode = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}