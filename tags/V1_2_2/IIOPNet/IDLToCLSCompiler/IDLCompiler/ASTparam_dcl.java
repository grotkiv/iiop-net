/* Generated By:JJTree: Do not edit this line. ASTparam_dcl.java */

package parser;

public class ASTparam_dcl extends SimpleNode {
  public ASTparam_dcl(int id) {
    super(id);
  }

  public ASTparam_dcl(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}
