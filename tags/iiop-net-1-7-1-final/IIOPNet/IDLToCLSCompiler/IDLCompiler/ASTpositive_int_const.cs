/* Generated By:JJTree: Do not edit this line. ASTpositive_int_const.cs */

using System;

namespace parser {

public class ASTpositive_int_const : SimpleNode {
  public ASTpositive_int_const(int id) : base(id) {
  }

  public ASTpositive_int_const(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}
