/* Generated By:JJTree: Do not edit this line. ASTtemplate_type_spec.cs */

using System;

namespace parser {

public class ASTtemplate_type_spec : SimpleNode {
  public ASTtemplate_type_spec(int id) : base(id) {
  }

  public ASTtemplate_type_spec(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
  
  public override string GetIdentification() {      
      return ((SimpleNode)jjtGetChild(0)).GetIdentification();
  }

}


}

