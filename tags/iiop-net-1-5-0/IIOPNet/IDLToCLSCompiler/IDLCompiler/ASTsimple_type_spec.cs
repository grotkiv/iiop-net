/* Generated By:JJTree: Do not edit this line. ASTsimple_type_spec.cs */

using System;

namespace parser {

public class ASTsimple_type_spec : SimpleNode {
  public ASTsimple_type_spec(int id) : base(id) {
  }

  public ASTsimple_type_spec(IDLParser p, int id) : base(p, id) {
  }


  /** Accept the visitor. **/
  public override Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}


}
