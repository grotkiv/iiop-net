/* Generated By:JJTree: Do not edit this line. ASTparam_dcl.cs */

using System;

namespace parser {

public class ASTparam_dcl : SimpleNode {
  public ASTparam_dcl(int id) : base(id) {
  }

  public ASTparam_dcl(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}

