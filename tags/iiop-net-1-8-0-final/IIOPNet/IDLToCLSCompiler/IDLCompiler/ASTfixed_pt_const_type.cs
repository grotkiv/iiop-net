/* Generated By:JJTree: Do not edit this line. ASTfixed_pt_const_type.cs */

using System;

namespace parser {

public class ASTfixed_pt_const_type : SimpleNode {
  public ASTfixed_pt_const_type(int id) : base(id) {
  }

  public ASTfixed_pt_const_type(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}

