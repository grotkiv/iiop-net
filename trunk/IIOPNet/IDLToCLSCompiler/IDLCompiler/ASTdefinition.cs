/* Generated By:JJTree: Do not edit this line. ASTdefinition.cs */

using System;

namespace parser {

public class ASTdefinition : SimpleNode {
  public ASTdefinition(int id) : base(id) {
  }

  public ASTdefinition(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
  
  public override string GetIdentification() {
      return ((SimpleNode)jjtGetParent()).GetIdentification();
  }

  
}


}

