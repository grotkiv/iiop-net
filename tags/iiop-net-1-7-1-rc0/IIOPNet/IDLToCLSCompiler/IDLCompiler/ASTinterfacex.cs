/* Generated By:JJTree: Do not edit this line. ASTinterfacex.cs */

using System;

namespace parser {

public class ASTinterfacex : SimpleNode {
  public ASTinterfacex(int id) : base(id) {
  }

  public ASTinterfacex(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
  
  public override string GetIdentification() {      
    return ((SimpleNode)jjtGetChild(0)).GetIdentification();      
  }
  
  public override string GetEmbedderDesc() {
    // is called by children for the defined in part
    return ((SimpleNode)jjtGetParent()).GetEmbedderDesc();
  }

}


}
