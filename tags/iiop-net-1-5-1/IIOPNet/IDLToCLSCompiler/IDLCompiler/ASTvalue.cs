/* Generated By:JJTree: Do not edit this line. ASTvalue.cs */

using System;

namespace parser {

public class ASTvalue : SimpleNode {
  public ASTvalue(int id) : base(id) {
  }

  public ASTvalue(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}
