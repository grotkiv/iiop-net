/* Generated By:JJTree: Do not edit this line. ASTenumerator.java */

package parser;

public class ASTenumerator extends SimpleNodeWithIdent {
  public ASTenumerator(int id) {
    super(id);
  }

  public ASTenumerator(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}