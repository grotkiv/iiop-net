/* Generated By:JJTree: Do not edit this line. ASTadd_expr.cs */

using System;

namespace parser {

public class ASTadd_expr : SimpleNode {
  public ASTadd_expr(int id) : base(id) {
  }

  public ASTadd_expr(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}

