/* Generated By:JJTree: Do not edit this line. ASTparam_type_spec.java */

package parser;

public class ASTparam_type_spec extends SimpleNode {
  public ASTparam_type_spec(int id) {
    super(id);
  }

  public ASTparam_type_spec(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}
