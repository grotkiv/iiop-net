/* Generated By:JJTree: Do not edit this line. ASTinit_param_delcs.cs */

using System;

namespace parser {

public class ASTinit_param_delcs : SimpleNode {
  public ASTinit_param_delcs(int id) : base(id) {
  }

  public ASTinit_param_delcs(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}

